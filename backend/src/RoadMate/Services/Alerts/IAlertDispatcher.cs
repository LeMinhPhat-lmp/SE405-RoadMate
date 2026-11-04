namespace RoadMate.Services.Alerts;

/// <param name="Deliveries">Log từng lần gửi (đã thêm vào DbContext, chưa SaveChanges).</param>
/// <param name="AllAcknowledged">true nếu mọi liên hệ đều đã bấm xác nhận -> không cần lặp nữa.</param>
public record DispatchResult(List<AlertDelivery> Deliveries, bool AllAcknowledged);

public interface IAlertDispatcher
{
  /// <summary>Gửi cảnh báo tới liên hệ chưa xác nhận, tạo token xác nhận nếu chưa có (chưa SaveChanges).</summary>
  Task<DispatchResult> DispatchAsync(Alert alert, User user, IReadOnlyList<EmergencyContact> contacts, bool isRepeat, CancellationToken ct);
  /// <summary>Báo "đã an toàn"/"đã có tín hiệu" cho những người đã nhận cảnh báo (chưa SaveChanges).</summary>
  Task<List<AlertDelivery>> NotifyClosedAsync(Alert alert, User user, IReadOnlyList<EmergencyContact> contacts, CancellationToken ct);
}
