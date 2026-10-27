namespace RoadMate.Services;

public interface IAlertService
{
  Task<Result<AlertDto>> CreateAsync(string uid, Guid tripId, CreateAlertRequest req, CancellationToken ct);
  Task<Result<List<AlertDto>>> ListAsync(string uid, Guid tripId, CancellationToken ct);
  Task<Result<AlertDto>> UpdateLocationAsync(string uid, Guid alertId, AlertLocationRequest req, CancellationToken ct);
  Task<Result<AlertDto>> CancelAsync(string uid, Guid alertId, CancellationToken ct);
  Task<Result<AlertDto>> ResolveAsync(string uid, Guid alertId, CancellationToken ct);
  Task ProcessDueRepeatsAsync(CancellationToken ct);
}

public class AlertService : IAlertService
{
  private readonly IUnitOfWork _uow; private readonly IAlertDispatcher _dispatcher; private readonly AlertOptions _opt;
  public AlertService(IUnitOfWork uow, IAlertDispatcher d, IOptions<AlertOptions> opt) { _uow = uow; _dispatcher = d; _opt = opt.Value; }

  public async Task<Result<AlertDto>> CreateAsync(string uid, Guid tripId, CreateAlertRequest req, CancellationToken ct)
  {
    if (string.IsNullOrWhiteSpace(req.ClientId) || req.ClientId.Length > 100) return Error.Validation("INVALID_CLIENT_ID", "clientId bắt buộc (≤100 ký tự).");
    if (!AlertType.All.Contains(req.Type)) return Error.Validation("INVALID_TYPE", $"type phải thuộc: {string.Join(", ", AlertType.All)}.");
    if (!Geo.ValidLatLng(req.Lat, req.Lng)) return Error.Validation("INVALID_LOCATION", "Toạ độ không hợp lệ.");

    // Idempotency: app retry khi mất mạng không tạo/gửi trùng.
    var existing = await _uow.Alerts.GetByClientIdAsync(req.ClientId, ct);
    if (existing is not null)
      return existing.UserId == uid ? existing.ToDto() : Error.Conflict("CLIENT_ID_TAKEN", "clientId đã được sử dụng.");

    var trip = await _uow.Trips.GetOwnedAsync(tripId, uid, ct);
    if (trip is null) return Errors.TripNotFound;
    if (trip.Status != TripStatus.Active) return Errors.TripNotActive;
    var user = await _uow.Users.GetByUidAsync(uid, ct);
    if (user is null) return Errors.UserNotSynced;

    var now = DateTime.UtcNow;
    var alert = new Alert
    {
      Id = Guid.NewGuid(),
      TripId = tripId,
      UserId = uid,
      ClientId = req.ClientId,
      Type = req.Type,
      Status = AlertStatus.Pending,
      Lat = req.Lat,
      Lng = req.Lng,
      Battery = req.Battery,
      CreatedAt = now,
      NextRepeatAt = now.AddMinutes(_opt.RepeatMinutes)
    };
    await _uow.Alerts.AddAsync(alert, ct);
    await _uow.SaveChangesAsync(ct);                       // lưu trước để không mất cảnh báo nếu gửi lỗi

    var contacts = await _uow.EmergencyContacts.GetAlertRecipientsAsync(tripId, uid, ct);
    var deliveries = await _dispatcher.DispatchAsync(alert, user, contacts, false, ct);
    alert.RepeatCount = 1;
    if (deliveries.Any(d => d.Status == DeliveryStatus.Sent)) alert.Status = AlertStatus.Sent;
    await _uow.SaveChangesAsync(ct);

    alert.Deliveries = deliveries;
    return alert.ToDto();
  }

  public async Task<Result<List<AlertDto>>> ListAsync(string uid, Guid tripId, CancellationToken ct)
  {
    if (await _uow.Trips.GetOwnedAsync(tripId, uid, ct) is null) return Errors.TripNotFound;
    return (await _uow.Alerts.ListByTripAsync(tripId, ct)).Select(a => a.ToDto()).ToList();
  }

  public async Task<Result<AlertDto>> UpdateLocationAsync(string uid, Guid alertId, AlertLocationRequest req, CancellationToken ct)
  {
    if (!Geo.ValidLatLng(req.Lat, req.Lng)) return Error.Validation("INVALID_LOCATION", "Toạ độ không hợp lệ.");
    var a = await _uow.Alerts.GetOwnedAsync(alertId, uid, ct);
    if (a is null) return Errors.AlertNotFound;
    if (!a.IsOpen) return Error.Conflict("ALERT_CLOSED", "Cảnh báo đã đóng.");
    a.Lat = req.Lat; a.Lng = req.Lng; a.Battery = req.Battery ?? a.Battery;
    await _uow.SaveChangesAsync(ct);
    return a.ToDto(false);
  }

  public Task<Result<AlertDto>> CancelAsync(string uid, Guid alertId, CancellationToken ct) => CloseAsync(uid, alertId, AlertStatus.Cancelled, ct);
  public Task<Result<AlertDto>> ResolveAsync(string uid, Guid alertId, CancellationToken ct) => CloseAsync(uid, alertId, AlertStatus.Resolved, ct);

  private async Task<Result<AlertDto>> CloseAsync(string uid, Guid alertId, string target, CancellationToken ct)
  {
    var a = await _uow.Alerts.GetOwnedAsync(alertId, uid, ct);
    if (a is null) return Errors.AlertNotFound;
    if (a.Status == target) return a.ToDto(false);                       // idempotent
    if (!a.IsOpen) return Error.Conflict("ALERT_CLOSED", $"Cảnh báo đã ở trạng thái {a.Status}.");

    var alreadyNotified = a.RepeatCount > 0;
    a.Status = target; a.ResolvedAt = DateTime.UtcNow; a.ResolvedBy = "user"; a.NextRepeatAt = null;
    if (alreadyNotified && a.User is not null)
    {
      var contacts = await _uow.EmergencyContacts.GetAlertRecipientsAsync(a.TripId, uid, ct);
      await _dispatcher.NotifySafeAsync(a, a.User, contacts, ct);      // báo "đã an toàn" cho người đã nhận SMS
    }
    await _uow.SaveChangesAsync(ct);
    return a.ToDto(false);
  }

  public async Task ProcessDueRepeatsAsync(CancellationToken ct)
  {
    var now = DateTime.UtcNow;
    var due = await _uow.Alerts.ListDueForRepeatAsync(now, 20, ct);
    foreach (var a in due)
    {
      if (a.Trip is null || a.User is null || a.Trip.Status == TripStatus.Finished)
      {
        a.Status = AlertStatus.Resolved; a.ResolvedAt = now; a.ResolvedBy = "system"; a.NextRepeatAt = null;
      }
      else if (a.RepeatCount >= _opt.MaxRepeats)
      {
        a.NextRepeatAt = null;                                       // dừng lặp để tránh spam SMS
      }
      else
      {
        var contacts = await _uow.EmergencyContacts.GetAlertRecipientsAsync(a.TripId, a.UserId, ct);
        var res = await _dispatcher.DispatchAsync(a, a.User, contacts, true, ct);
        if (a.Status == AlertStatus.Pending && res.Any(d => d.Status == DeliveryStatus.Sent)) a.Status = AlertStatus.Sent;
        a.RepeatCount++; a.NextRepeatAt = now.AddMinutes(_opt.RepeatMinutes);
      }
      await _uow.SaveChangesAsync(ct);
    }
  }
}
