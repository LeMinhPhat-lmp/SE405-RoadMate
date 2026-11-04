namespace RoadMate.Services.Alerts;

public class AlertRepeatProcessor(IUnitOfWork uow, IAlertDispatcher dispatcher, IOptions<AlertOptions> options, TimeProvider clock,
  ILogger<AlertRepeatProcessor> log) : IAlertRepeatProcessor
{
  private const int BatchSize = 20;

  public async Task ProcessDueAsync(CancellationToken ct)
  {
    var now = clock.UtcNow();
    var due = await uow.AlertRepository.ListDueForRepeatAsync(now, BatchSize, ct);
    foreach (var alert in due)
    {
      try
      {
        await ProcessOneAsync(alert, now, ct);
        await uow.SaveChangesAsync(ct);
      }
      catch (Exception ex) when (ex is not OperationCanceledException)
      {
        // Một cảnh báo lỗi không được chặn cả hàng đợi: bỏ thay đổi dở dang, lùi lịch cảnh báo này, các cảnh báo còn lại xử lý ở lần chạy sau.
        log.LogError(ex, "Lặp cảnh báo {AlertId} thất bại, sẽ thử lại sau", alert.Id);
        uow.DiscardChanges();
        await PostponeAsync(alert.Id, now, ct);
        return;
      }
    }
  }

  private async Task ProcessOneAsync(Alert a, DateTime now, CancellationToken ct)
  {
    if (a.Trip is null || a.User is null || a.Trip.Status == TripStatus.Finished)
    {
      a.Close(AlertStatus.Resolved, AlertResolver.System, now);
      log.LogInformation("Đóng cảnh báo {AlertId}: chuyến đã kết thúc hoặc không còn tồn tại", a.Id);
      return;
    }
    if (a.RepeatCount >= options.Value.MaxRepeats)
    {
      a.NextRepeatAt = null;                                        // dừng lặp để tránh spam SMS
      log.LogWarning("Cảnh báo {AlertId} đạt giới hạn {Max} lần lặp, dừng gửi", a.Id, options.Value.MaxRepeats);
      return;
    }

    var contacts = await uow.EmergencyContactRepository.GetAlertRecipientsAsync(a.TripId, a.UserId, ct);
    var res = await dispatcher.DispatchAsync(a, a.User, contacts, true, ct);
    if (a.Status == AlertStatus.Pending && res.Deliveries.Any(d => d.Status == DeliveryStatus.Sent)) a.Status = AlertStatus.Sent;
    a.RepeatCount++;
    var interval = AlertPolicy.RepeatInterval(a.Type, options.Value);
    a.NextRepeatAt = res.AllAcknowledged || interval is null ? null : now + interval;
  }

  private async Task PostponeAsync(Guid alertId, DateTime now, CancellationToken ct)
  {
    try
    {
      var fresh = await uow.AlertRepository.GetByIdAsync(alertId, true, ct);
      if (fresh is null) return;
      fresh.NextRepeatAt = now.AddMinutes(options.Value.RepeatMinutes);
      await uow.SaveChangesAsync(ct);
    }
    catch (Exception ex) when (ex is not OperationCanceledException)
    {
      log.LogError(ex, "Không lùi được lịch lặp của cảnh báo {AlertId}", alertId);
      uow.DiscardChanges();
    }
  }
}
