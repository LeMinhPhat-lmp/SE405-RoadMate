namespace RoadMate.Services.Notifications;

public interface IContactNotifier
{
  /// <summary>
  /// Gửi SMS (+ push nếu liên hệ đã liên kết tài khoản) tới từng liên hệ. KHÔNG lưu DB: trả về log từng lần gửi để nơi gọi quyết định lưu.
  /// <paramref name="alertId"/> = Guid.Empty khi tin nhắn không thuộc cảnh báo nào (vd: tin nhắn thử).
  /// </summary>
  Task<List<AlertDelivery>> NotifyAsync(Guid alertId, IReadOnlyList<EmergencyContact> contacts, NotificationContent content, NotificationMode mode, CancellationToken ct);

  /// <summary>Như trên nhưng nội dung riêng cho từng liên hệ (vd: link xác nhận riêng).</summary>
  Task<List<AlertDelivery>> NotifyAsync(Guid alertId, IReadOnlyList<EmergencyContact> contacts, Func<EmergencyContact, NotificationContent> contentFor, NotificationMode mode, CancellationToken ct);
}
