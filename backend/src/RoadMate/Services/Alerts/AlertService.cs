using RoadMate.Data;

namespace RoadMate.Services.Alerts;

public class AlertService(IUnitOfWork uow, IAlertDispatcher dispatcher, IOptions<AlertOptions> options, TimeProvider clock,
  ILogger<AlertService> log) : IAlertService
{
  public Task<Result<AlertDto>> CreateAsync(Guid userId, Guid tripId, CreateAlertRequest req, CancellationToken ct) =>
    RaiseAsync(userId, tripId, req, null, ct);

  public Task<Result<AlertDto>> CreateSimulatedAsync(Guid userId, Guid tripId, CreateAlertRequest req, string simulationMode, CancellationToken ct) =>
    RaiseAsync(userId, tripId, req, simulationMode, ct);

  public Task<Result<AlertDto>> RaiseSystemAsync(Guid userId, Guid tripId, string type, double lat, double lng, int? battery, CancellationToken ct) =>
    RaiseAsync(userId, tripId, new CreateAlertRequest($"sys-{type}-{Guid.NewGuid():N}", type, lat, lng, battery), null, ct);

  private async Task<Result<AlertDto>> RaiseAsync(Guid userId, Guid tripId, CreateAlertRequest req, string? simulationMode, CancellationToken ct)
  {
    var valid = CreateAlertRequestValidator.Validate(req);
    if (valid.IsFailure) return valid.Error;

    // Idempotency: app retry khi mất mạng không tạo/gửi trùng.
    var existing = await uow.AlertRepository.GetByClientIdAsync(req.ClientId, ct);
    if (existing is not null) return ExistingFor(existing, userId);

    var trip = await uow.TripRepository.GetOwnedActiveAsync(tripId, userId, false, ct);
    if (trip.IsFailure) return trip.Error;
    var user = await uow.UserRepository.GetByIdAsync(userId, false, ct);
    if (user is null) return Errors.UserNotSynced;

    var interval = AlertPolicy.RepeatInterval(req.Type, options.Value);
    var alert = req.ToEntity(tripId, userId, interval is null ? null : clock.UtcNow() + interval, simulationMode);
    await uow.AlertRepository.AddAsync(alert, ct);
    try
    {
      await uow.SaveChangesAsync(ct);                     // lưu trước để không mất cảnh báo nếu gửi lỗi
    }
    catch (DbUpdateException ex) when (ex.IsUniqueViolation())
    {
      // Hai request cùng clientId chạy song song: bên chậm hơn thua ở unique index -> trả lại cảnh báo của bên thắng.
      uow.DiscardChanges();
      var winner = await uow.AlertRepository.GetByClientIdAsync(req.ClientId, ct);
      if (winner is null) throw;
      log.LogInformation("clientId {ClientId} bị gửi trùng đồng thời, trả lại cảnh báo {AlertId}", req.ClientId, winner.Id);
      return ExistingFor(winner, userId);
    }

    log.LogWarning("Cảnh báo {AlertType} {AlertId} cho chuyến {TripId} (user {UserId}, giả lập: {Simulation})",
      alert.Type, alert.Id, tripId, userId, simulationMode ?? "không");
    if (!AlertPolicy.NotifiesContacts(alert.Type)) return alert.ToDto();

    var contacts = await uow.EmergencyContactRepository.GetAlertRecipientsAsync(tripId, userId, ct);
    if (contacts.Count == 0) log.LogWarning("Cảnh báo {AlertId} không có liên hệ nào để gửi", alert.Id);

    var dispatched = await dispatcher.DispatchAsync(alert, user, contacts, false, ct);
    alert.RepeatCount = 1;
    if (dispatched.Deliveries.Any(d => d.Status == DeliveryStatus.Sent)) alert.Status = AlertStatus.Sent;
    if (contacts.Count == 0 || dispatched.AllAcknowledged) alert.NextRepeatAt = null;
    await uow.SaveChangesAsync(ct);

    alert.Deliveries = dispatched.Deliveries;
    log.LogInformation("Cảnh báo {AlertId}: {Sent}/{Total} lượt gửi thành công", alert.Id,
      dispatched.Deliveries.Count(d => d.Status == DeliveryStatus.Sent), dispatched.Deliveries.Count);
    return alert.ToDto();
  }

  private static Result<AlertDto> ExistingFor(Alert existing, Guid userId) =>
    existing.UserId == userId ? existing.ToDto() : Error.Conflict("CLIENT_ID_TAKEN", "clientId đã được sử dụng.");

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
    if (!a.IsOpen) return Errors.AlertClosed;
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
    if (!a.IsOpen) return Error.Conflict(Errors.AlertClosed.Code, $"Cảnh báo đã ở trạng thái {a.Status}.");

    await CloseAndNotifyAsync(a, targetStatus, AlertResolver.User, ct);
    await uow.SaveChangesAsync(ct);
    log.LogInformation("Cảnh báo {AlertId} được đóng: {Status} bởi {ResolvedBy}", a.Id, targetStatus, AlertResolver.User);
    return a.ToDto(false);
  }

  public async Task CloseOpenAsync(Guid tripId, string? type, string resolvedBy, CancellationToken ct)
  {
    var open = type is null
      ? await uow.AlertRepository.ListOpenByTripAsync(tripId, ct)
      : await uow.AlertRepository.ListOpenByTypeAsync(tripId, type, ct);
    if (open.Count == 0) return;

    foreach (var alert in open)
    {
      alert.User ??= await uow.UserRepository.GetByIdAsync(alert.UserId, false, ct);
      await CloseAndNotifyAsync(alert, AlertStatus.Resolved, resolvedBy, ct);
    }
    await uow.SaveChangesAsync(ct);
    log.LogInformation("Đã đóng {Count} cảnh báo đang mở của chuyến {TripId} (loại {Type}) bởi {ResolvedBy}", open.Count, tripId, type ?? "*", resolvedBy);
  }

  /// <summary>Đóng cảnh báo; nếu người thân đã nhận tin (RepeatCount > 0) thì báo lại "đã an toàn"/"đã có tín hiệu". Chưa SaveChanges.</summary>
  private async Task CloseAndNotifyAsync(Alert alert, string status, string resolvedBy, CancellationToken ct)
  {
    var alreadyNotified = alert.RepeatCount > 0 && AlertPolicy.NotifiesContacts(alert.Type);
    alert.Close(status, resolvedBy, clock.UtcNow());
    if (!alreadyNotified || alert.User is null) return;
    var contacts = await uow.EmergencyContactRepository.GetAlertRecipientsAsync(alert.TripId, alert.UserId, ct);
    await dispatcher.NotifyClosedAsync(alert, alert.User, contacts, ct);
  }
}
