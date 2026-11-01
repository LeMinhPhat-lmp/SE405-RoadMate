namespace RoadMate.Services.Alerts;

public class AlertService(IUnitOfWork uow, IAlertDispatcher dispatcher, IOptions<AlertOptions> options) : IAlertService
{
  public async Task<Result<AlertDto>> CreateAsync(Guid userId, Guid tripId, CreateAlertRequest req, CancellationToken ct)
  {
    var valid = CreateAlertRequestValidator.Validate(req);
    if (valid.IsFailure) return valid.Error;

    // Idempotency: app retry khi mất mạng không tạo/gửi trùng.
    var existing = await uow.AlertRepository.GetByClientIdAsync(req.ClientId, ct);
    if (existing is not null)
      return existing.UserId == userId ? existing.ToDto() : Error.Conflict("CLIENT_ID_TAKEN", "clientId đã được sử dụng.");

    var trip = await uow.TripRepository.GetOwnedActiveAsync(tripId, userId, false, ct);
    if (trip.IsFailure) return trip.Error;
    var user = await uow.UserRepository.GetByIdAsync(userId, false, ct);
    if (user is null) return Errors.UserNotSynced;

    var alert = req.ToEntity(tripId, userId, DateTime.UtcNow.AddMinutes(options.Value.RepeatMinutes));
    await uow.AlertRepository.AddAsync(alert, ct);
    await uow.SaveChangesAsync(ct);                       // lưu trước để không mất cảnh báo nếu gửi lỗi

    var contacts = await uow.EmergencyContactRepository.GetAlertRecipientsAsync(tripId, userId, ct);
    var deliveries = await dispatcher.DispatchAsync(alert, user, contacts, false, ct);
    alert.RepeatCount = 1;
    if (deliveries.Any(d => d.Status == DeliveryStatus.Sent)) alert.Status = AlertStatus.Sent;
    await uow.SaveChangesAsync(ct);

    alert.Deliveries = deliveries;
    return alert.ToDto();
  }

  public async Task<Result<List<AlertDto>>> ListAsync(Guid userId, Guid tripId, CancellationToken ct)
  {
    if (await uow.TripRepository.GetOwnedAsync(tripId, userId, ct) is null) return Errors.TripNotFound;
    return (await uow.AlertRepository.ListByTripAsync(tripId, ct)).Select(a => a.ToDto()).ToList();
  }

  public async Task<Result<AlertDto>> UpdateLocationAsync(Guid userId, Guid alertId, AlertLocationRequest req, CancellationToken ct)
  {
    if (!Geo.ValidLatLng(req.Lat, req.Lng)) return Errors.InvalidLocation;
    var a = await uow.AlertRepository.GetOwnedAsync(alertId, userId, ct);
    if (a is null) return Errors.AlertNotFound;
    if (!a.IsOpen) return Error.Conflict("ALERT_CLOSED", "Cảnh báo đã đóng.");
    a.Lat = req.Lat; a.Lng = req.Lng; a.Battery = req.Battery ?? a.Battery;
    await uow.SaveChangesAsync(ct);
    return a.ToDto(false);
  }

  public async Task<Result<AlertDto>> CloseAsync(Guid userId, Guid alertId, string targetStatus, CancellationToken ct)
  {
    if (targetStatus is not (AlertStatus.Cancelled or AlertStatus.Resolved))
      return Error.Validation("INVALID_STATUS", "Chỉ đóng được với trạng thái cancelled hoặc resolved.");
    var a = await uow.AlertRepository.GetOwnedAsync(alertId, userId, ct);
    if (a is null) return Errors.AlertNotFound;
    if (a.Status == targetStatus) return a.ToDto(false);                       // idempotent
    if (!a.IsOpen) return Error.Conflict("ALERT_CLOSED", $"Cảnh báo đã ở trạng thái {a.Status}.");

    var alreadyNotified = a.RepeatCount > 0;
    a.Close(targetStatus, AlertResolver.User, DateTime.UtcNow);
    if (alreadyNotified && a.User is not null)
    {
      var contacts = await uow.EmergencyContactRepository.GetAlertRecipientsAsync(a.TripId, userId, ct);
      await dispatcher.NotifySafeAsync(a, a.User, contacts, ct);      // báo "đã an toàn" cho người đã nhận SMS
    }
    await uow.SaveChangesAsync(ct);
    return a.ToDto(false);
  }
}
