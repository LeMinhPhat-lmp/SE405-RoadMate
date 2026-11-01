namespace RoadMate.Services.Notifications;

public interface IContactNotifier
{
  /// <summary>
  /// Gửi SMS (+ push nếu liên hệ đã liên kết tài khoản) tới từng liên hệ. KHÔNG lưu DB: trả về log từng lần gửi để nơi gọi quyết định lưu.
  /// <paramref name="alertId"/> = Guid.Empty khi tin nhắn không thuộc cảnh báo nào (vd: tin nhắn thử).
  /// </summary>
  Task<List<AlertDelivery>> NotifyAsync(Guid alertId, IReadOnlyList<EmergencyContact> contacts, NotificationContent content, CancellationToken ct);
}
