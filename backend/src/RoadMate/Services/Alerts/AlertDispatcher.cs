using RoadMate.Services.Notifications;

namespace RoadMate.Services.Alerts;

public class AlertDispatcher(IContactNotifier notifier, IUnitOfWork uow) : IAlertDispatcher
{
  public async Task<List<AlertDelivery>> DispatchAsync(Alert alert, User user, IReadOnlyList<EmergencyContact> contacts, bool isRepeat, CancellationToken ct)
  {
    var deliveries = await notifier.NotifyAsync(alert.Id, contacts, AlertMessages.ForAlert(alert, user, isRepeat), ct);
    await uow.AlertDeliveryRepository.AddRangeAsync(deliveries, ct);
    return deliveries;
  }

  public async Task<List<AlertDelivery>> NotifySafeAsync(Alert alert, User user, IReadOnlyList<EmergencyContact> contacts, CancellationToken ct)
  {
    var deliveries = await notifier.NotifyAsync(alert.Id, contacts, AlertMessages.Safe(alert, user), ct);
    await uow.AlertDeliveryRepository.AddRangeAsync(deliveries, ct);
    return deliveries;
  }
}
