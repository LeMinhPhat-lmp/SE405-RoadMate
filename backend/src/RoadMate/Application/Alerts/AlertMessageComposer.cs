using System.Globalization;
using System.Text;
using RoadMate.Application.Common;
using RoadMate.Domain.Alerts;
using RoadMate.Domain.Common;

namespace RoadMate.Application.Alerts;

/// <summary>Pure composition of SMS/push text (Vietnamese, legacy wording). No I/O, no clock: <c>now</c> is passed in.</summary>
public static class AlertMessageComposer
{
  /// <param name="address">Reverse-geocoded address; null when unavailable.</param>
  /// <param name="ackUrl">The contact's private acknowledgement link; null when Tracking:AckUrl is not configured.</param>
  public static NotificationContent ForAlert(Alert a, IAlertTypePolicy policy, OwnerInfo owner, bool isRepeat, DateTimeOffset now, string? address = null, string? ackUrl = null)
  {
    var sb = new StringBuilder("[RoadMate]");
    if (isRepeat) sb.Append(" CẬP NHẬT");
    sb.Append(CultureInfo.InvariantCulture, $" {owner.Name} {policy.ReasonText}. Vị trí: https://maps.google.com/?q={a.Lat.ToCoordinate()},{a.Lng.ToCoordinate()}");
    if (!string.IsNullOrWhiteSpace(address)) sb.Append($" (gần {address})");
    sb.Append(CultureInfo.InvariantCulture, $". Lúc {now.ToOffset(TimeSpan.FromHours(7)):HH:mm dd/MM}.");
    if (!string.IsNullOrWhiteSpace(owner.BloodType)) sb.Append($" Nhóm máu: {owner.BloodType}.");
    if (!string.IsNullOrWhiteSpace(owner.PlateNumber)) sb.Append($" Biển số: {owner.PlateNumber}.");
    if (a.Battery is not null) sb.Append(CultureInfo.InvariantCulture, $" Pin: {a.Battery}%.");
    sb.Append(" Hãy liên lạc ngay hoặc gọi 113/115.");
    if (!string.IsNullOrWhiteSpace(ackUrl)) sb.Append($" Đã liên lạc được? Xác nhận: {ackUrl}");
    return new NotificationContent(sb.ToString(), isRepeat ? "RoadMate: CẬP NHẬT cảnh báo" : "RoadMate: CẢNH BÁO KHẨN CẤP",
      new Dictionary<string, string>
      {
        ["type"] = "alert",
        ["alertId"] = a.Id.ToString(),
        ["tripId"] = a.TripId.ToString(),
        ["alertType"] = EnumWire.ToWire(a.Type),
        ["ownerName"] = owner.Name,
        ["lat"] = a.Lat.ToCoordinate(),
        ["lng"] = a.Lng.ToCoordinate()
      });
  }

  /// <summary>"Is safe" notice, or (lostSignal) "signal is back".</summary>
  public static NotificationContent Closed(Alert a, string ownerName) => a.Type == AlertType.LostSignal
    ? new($"[RoadMate] {ownerName} đã có tín hiệu trở lại. Vui lòng bỏ qua các cảnh báo mất tín hiệu trước.", "RoadMate: đã có tín hiệu",
        new Dictionary<string, string> { ["type"] = "signal_restored", ["alertId"] = a.Id.ToString() })
    : new($"[RoadMate] {ownerName} đã xác nhận AN TOÀN. Vui lòng bỏ qua các cảnh báo trước.", "RoadMate: đã an toàn",
        new Dictionary<string, string> { ["type"] = "safe", ["alertId"] = a.Id.ToString() });
}
