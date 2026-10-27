namespace RoadMate.Services.Interfaces;

public interface IAlertDispatcher
{
  Task<List<AlertDelivery>> DispatchAsync(Alert alert, User user, IReadOnlyList<EmergencyContact> contacts, bool isRepeat, CancellationToken ct);
  Task<List<AlertDelivery>> NotifySafeAsync(Alert alert, User user, IReadOnlyList<EmergencyContact> contacts, CancellationToken ct);
  Task<List<AlertDelivery>> SendTestAsync(User user, EmergencyContact contact, CancellationToken ct);
}
