using RoadMate.Services.Notifications;

namespace RoadMate.Services.Alerts;

/// <summary>Soạn nội dung SMS/push cho các loại thông báo của cảnh báo (hàm thuần, không I/O).</summary>
public static class AlertMessages
{
  public static NotificationContent ForAlert(Alert a, User u, bool isRepeat)
  {
    var reason = a.Type switch
    {
      AlertType.Crash => "nghi gặp va chạm/té ngã và không phản hồi",
      AlertType.Sos => "vừa bấm nút SOS",
      AlertType.Idle => "dừng quá lâu và không phản hồi",
      AlertType.OffRoute => "đang lệch khỏi lộ trình",
      AlertType.LowBattery => "sắp hết pin",
      AlertType.LostSignal => "mất tín hiệu quá lâu",
      AlertType.LateReturn => "chưa về dù đã quá giờ dự kiến",
      _ => "có sự cố"
    };
    var sb = new StringBuilder("[RoadMate]");
    if (isRepeat) sb.Append(" CẬP NHẬT");
    sb.Append($" {u.Name} {reason}. Vị trí: https://maps.google.com/?q={a.Lat.ToCoordinate()},{a.Lng.ToCoordinate()}");
    sb.Append($". Lúc {DateTime.UtcNow.AddHours(7):HH:mm dd/MM}.");
    if (!string.IsNullOrWhiteSpace(u.BloodType)) sb.Append($" Nhóm máu: {u.BloodType}.");
    if (!string.IsNullOrWhiteSpace(u.PlateNumber)) sb.Append($" Biển số: {u.PlateNumber}.");
    if (a.Battery is not null) sb.Append($" Pin: {a.Battery}%.");
    sb.Append(" Hãy liên lạc ngay hoặc gọi 113/115.");

    return new NotificationContent(sb.ToString(),
      isRepeat ? "RoadMate: CẬP NHẬT cảnh báo" : "RoadMate: CẢNH BÁO KHẨN CẤP",
      new Dictionary<string, string> { ["type"] = "alert", ["alertId"] = a.Id.ToString(), ["tripId"] = a.TripId.ToString() });
  }

  public static NotificationContent Safe(Alert a, User u) => new(
    $"[RoadMate] {u.Name} đã xác nhận AN TOÀN. Vui lòng bỏ qua các cảnh báo trước.",
    "RoadMate: đã an toàn",
    new Dictionary<string, string> { ["type"] = "safe", ["alertId"] = a.Id.ToString() });
}
