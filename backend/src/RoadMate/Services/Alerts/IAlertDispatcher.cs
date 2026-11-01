namespace RoadMate.Services.Alerts;

public interface IAlertDispatcher
{
  /// <summary>Gửi cảnh báo tới liên hệ và ghi log từng lần gửi vào DB (chưa SaveChanges).</summary>
  Task<List<AlertDelivery>> DispatchAsync(Alert alert, User user, IReadOnlyList<EmergencyContact> contacts, bool isRepeat, CancellationToken ct);
  /// <summary>Báo "đã an toàn" cho những người đã nhận cảnh báo (chưa SaveChanges).</summary>
  Task<List<AlertDelivery>> NotifySafeAsync(Alert alert, User user, IReadOnlyList<EmergencyContact> contacts, CancellationToken ct);
}
