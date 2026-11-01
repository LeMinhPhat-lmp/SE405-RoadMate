namespace RoadMate.Services.Alerts;

public class AlertRepeatProcessor(IUnitOfWork uow, IAlertDispatcher dispatcher, IOptions<AlertOptions> options) : IAlertRepeatProcessor
{
  public async Task ProcessDueAsync(CancellationToken ct)
  {
    var now = DateTime.UtcNow;
    var due = await uow.AlertRepository.ListDueForRepeatAsync(now, 20, ct);
    foreach (var a in due)
    {
      if (a.Trip is null || a.User is null || a.Trip.Status == TripStatus.Finished)
      {
        a.Close(AlertStatus.Resolved, AlertResolver.System, now);
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
