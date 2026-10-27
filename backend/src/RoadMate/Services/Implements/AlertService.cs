namespace RoadMate.Services.Implements;

public class AlertService(IUnitOfWork uow, IAlertDispatcher dispatcher, IOptions<AlertOptions> options) : IAlertService
{
  public async Task<Result<AlertDto>> CreateAsync(Guid userId, Guid tripId, CreateAlertRequest req, CancellationToken ct)
  {
    if (string.IsNullOrWhiteSpace(req.ClientId) || req.ClientId.Length > 100) return Error.Validation("INVALID_CLIENT_ID", "clientId bắt buộc (≤100 ký tự).");
    if (!AlertType.All.Contains(req.Type)) return Error.Validation("INVALID_TYPE", $"type phải thuộc: {string.Join(", ", AlertType.All)}.");
    if (!Geo.ValidLatLng(req.Lat, req.Lng)) return Error.Validation("INVALID_LOCATION", "Toạ độ không hợp lệ.");

    // Idempotency: app retry khi mất mạng không tạo/gửi trùng.
    var existing = await uow.AlertRepository.GetByClientIdAsync(req.ClientId, ct);
    if (existing is not null)
      return existing.UserId == userId ? existing.ToDto() : Error.Conflict("CLIENT_ID_TAKEN", "clientId đã được sử dụng.");

    var trip = await uow.TripRepository.GetOwnedAsync(tripId, userId, ct);
    if (trip is null) return Errors.TripNotFound;
    if (trip.Status != TripStatus.Active) return Errors.TripNotActive;
    var user = await uow.UserRepository.GetByIdAsync(userId, false, ct);
    if (user is null) return Errors.UserNotSynced;

    var now = DateTime.UtcNow;
    var alert = new Alert
    {
      Id = Guid.NewGuid(),
      TripId = tripId,
      UserId = userId,
      ClientId = req.ClientId,
      Type = req.Type,
      Status = AlertStatus.Pending,
      Lat = req.Lat,
      Lng = req.Lng,
      Battery = req.Battery,
      NextRepeatAt = now.AddMinutes(options.Value.RepeatMinutes)
    };
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
    if (!Geo.ValidLatLng(req.Lat, req.Lng)) return Error.Validation("INVALID_LOCATION", "Toạ độ không hợp lệ.");
    var a = await uow.AlertRepository.GetOwnedAsync(alertId, userId, ct);
    if (a is null) return Errors.AlertNotFound;
    if (!a.IsOpen) return Error.Conflict("ALERT_CLOSED", "Cảnh báo đã đóng.");
    a.Lat = req.Lat; a.Lng = req.Lng; a.Battery = req.Battery ?? a.Battery;
    await uow.SaveChangesAsync(ct);
    return a.ToDto(false);
  }

  public Task<Result<AlertDto>> CancelAsync(Guid userId, Guid alertId, CancellationToken ct) => CloseAsync(userId, alertId, AlertStatus.Cancelled, ct);
  public Task<Result<AlertDto>> ResolveAsync(Guid userId, Guid alertId, CancellationToken ct) => CloseAsync(userId, alertId, AlertStatus.Resolved, ct);

  private async Task<Result<AlertDto>> CloseAsync(Guid userId, Guid alertId, string target, CancellationToken ct)
  {
    var a = await uow.AlertRepository.GetOwnedAsync(alertId, userId, ct);
    if (a is null) return Errors.AlertNotFound;
    if (a.Status == target) return a.ToDto(false);                       // idempotent
    if (!a.IsOpen) return Error.Conflict("ALERT_CLOSED", $"Cảnh báo đã ở trạng thái {a.Status}.");

    var alreadyNotified = a.RepeatCount > 0;
    a.Status = target; a.ResolvedAt = DateTime.UtcNow; a.ResolvedBy = "user"; a.NextRepeatAt = null;
    if (alreadyNotified && a.User is not null)
    {
      var contacts = await uow.EmergencyContactRepository.GetAlertRecipientsAsync(a.TripId, userId, ct);
      await dispatcher.NotifySafeAsync(a, a.User, contacts, ct);      // báo "đã an toàn" cho người đã nhận SMS
    }
    await uow.SaveChangesAsync(ct);
    return a.ToDto(false);
  }

  public async Task ProcessDueRepeatsAsync(CancellationToken ct)
  {
    var now = DateTime.UtcNow;
    var due = await uow.AlertRepository.ListDueForRepeatAsync(now, 20, ct);
    foreach (var a in due)
    {
      if (a.Trip is null || a.User is null || a.Trip.Status == TripStatus.Finished)
      {
        a.Status = AlertStatus.Resolved; a.ResolvedAt = now; a.ResolvedBy = "system"; a.NextRepeatAt = null;
      }
      else if (a.RepeatCount >= options.Value.MaxRepeats)
      {
        a.NextRepeatAt = null;                                       // dừng lặp để tránh spam SMS
      }
      else
      {
        var contacts = await uow.EmergencyContactRepository.GetAlertRecipientsAsync(a.TripId, a.UserId, ct);
        var res = await dispatcher.DispatchAsync(a, a.User, contacts, true, ct);
        if (a.Status == AlertStatus.Pending && res.Any(d => d.Status == DeliveryStatus.Sent)) a.Status = AlertStatus.Sent;
        a.RepeatCount++; a.NextRepeatAt = now.AddMinutes(options.Value.RepeatMinutes);
      }
      await uow.SaveChangesAsync(ct);
    }
  }
}
