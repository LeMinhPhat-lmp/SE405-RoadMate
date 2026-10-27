using RoadMate.Infrastructure;

namespace RoadMate.Services;

/// <summary>Soạn nội dung và gửi SMS + push cho liên hệ khẩn cấp, ghi log vào AlertDelivery (chưa SaveChanges).</summary>
public interface IAlertDispatcher
{
  Task<List<AlertDelivery>> DispatchAsync(Alert alert, User user, IReadOnlyList<EmergencyContact> contacts, bool isRepeat, CancellationToken ct);
  Task<List<AlertDelivery>> NotifySafeAsync(Alert alert, User user, IReadOnlyList<EmergencyContact> contacts, CancellationToken ct);
  Task<List<AlertDelivery>> SendTestAsync(User user, EmergencyContact contact, CancellationToken ct);
}

public class AlertDispatcher : IAlertDispatcher
{
  private readonly ISmsSender _sms; private readonly IPushSender _push; private readonly IUnitOfWork _uow; private readonly SmsOptions _opt;
  public AlertDispatcher(ISmsSender sms, IPushSender push, IUnitOfWork uow, IOptions<SmsOptions> opt)
  { _sms = sms; _push = push; _uow = uow; _opt = opt.Value; }

  public Task<List<AlertDelivery>> DispatchAsync(Alert alert, User user, IReadOnlyList<EmergencyContact> contacts, bool isRepeat, CancellationToken ct)
    => SendAllAsync(alert.Id, contacts, BuildAlertText(alert, user, isRepeat),
      isRepeat ? "RoadMate: CẬP NHẬT cảnh báo" : "RoadMate: CẢNH BÁO KHẨN CẤP",
      new Dictionary<string, string> { ["type"] = "alert", ["alertId"] = alert.Id.ToString(), ["tripId"] = alert.TripId.ToString() }, ct);

  public Task<List<AlertDelivery>> NotifySafeAsync(Alert alert, User user, IReadOnlyList<EmergencyContact> contacts, CancellationToken ct)
    => SendAllAsync(alert.Id, contacts, $"[RoadMate] {user.Name} đã xác nhận AN TOÀN. Vui lòng bỏ qua các cảnh báo trước.",
      "RoadMate: đã an toàn", new Dictionary<string, string> { ["type"] = "safe", ["alertId"] = alert.Id.ToString() }, ct);

  public async Task<List<AlertDelivery>> SendTestAsync(User user, EmergencyContact contact, CancellationToken ct)
  {
    var list = await SendAllAsync(Guid.Empty, new[] { contact }, $"[RoadMate] Tin nhắn thử: {user.Name} đã thêm bạn làm người liên hệ khẩn cấp.",
      "RoadMate: tin nhắn thử", new Dictionary<string, string> { ["type"] = "test" }, ct, persist: false);
    return list;
  }

  private async Task<List<AlertDelivery>> SendAllAsync(Guid alertId, IReadOnlyList<EmergencyContact> contacts, string text, string title,
    IDictionary<string, string> data, CancellationToken ct, bool persist = true)
  {
    var smsText = _opt.StripDiacritics ? TextHelper.StripDiacritics(text) : text;
    var list = new List<AlertDelivery>();
    foreach (var c in contacts)
    {
      var r = await _sms.SendAsync(c.Phone, smsText, ct);
      list.Add(Make(alertId, c.Id, DeliveryChannel.Sms, r));
      if (!string.IsNullOrEmpty(c.LinkedUser?.FcmToken))
      {
        var p = await _push.SendAsync(c.LinkedUser!.FcmToken!, title, text, data, ct);
        list.Add(Make(alertId, c.Id, DeliveryChannel.Push, p));
      }
    }
    if (persist) foreach (var d in list) await _uow.AlertDeliveries.AddAsync(d, ct);
    return list;
  }

  private static AlertDelivery Make(Guid alertId, Guid contactId, string channel, Result<string> r) => new()
  {
    Id = Guid.NewGuid(),
    AlertId = alertId,
    ContactId = contactId,
    Channel = channel,
    SentAt = DateTime.UtcNow,
    Status = r.IsSuccess ? DeliveryStatus.Sent : DeliveryStatus.Failed,
    ProviderMessageId = r.IsSuccess ? r.Value : null,
    Error = r.IsSuccess ? null : r.Error.Message
  };

  private static string BuildAlertText(Alert a, User u, bool isRepeat)
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
    var ic = CultureInfo.InvariantCulture;
    var sb = new StringBuilder("[RoadMate]");
    if (isRepeat) sb.Append(" CẬP NHẬT");
    sb.Append($" {u.Name} {reason}. Vị trí: https://maps.google.com/?q={a.Lat.ToString("F6", ic)},{a.Lng.ToString("F6", ic)}");
    sb.Append($". Lúc {DateTime.UtcNow.AddHours(7):HH:mm dd/MM}.");
    if (!string.IsNullOrWhiteSpace(u.BloodType)) sb.Append($" Nhóm máu: {u.BloodType}.");
    if (!string.IsNullOrWhiteSpace(u.PlateNumber)) sb.Append($" Biển số: {u.PlateNumber}.");
    if (a.Battery is not null) sb.Append($" Pin: {a.Battery}%.");
    sb.Append(" Hãy liên lạc ngay hoặc gọi 113/115.");
    return sb.ToString();
  }
}
