using RoadMate.Infrastructure.Diagnostics;
using RoadMate.Infrastructure.Notifications;

namespace RoadMate.Services.Notifications;

public class ContactNotifier(ISmsSender sms, IPushSender push, IOptions<SmsOptions> smsOptions, IOptions<DebugOptions> debugOptions,
  ILogger<ContactNotifier> log) : IContactNotifier
{
  private const string DryRunMessageId = "dry-run";

  public Task<List<AlertDelivery>> NotifyAsync(Guid alertId, IReadOnlyList<EmergencyContact> contacts, NotificationContent content, NotificationMode mode, CancellationToken ct) =>
    NotifyAsync(alertId, contacts, _ => content, mode, ct);

  public async Task<List<AlertDelivery>> NotifyAsync(Guid alertId, IReadOnlyList<EmergencyContact> contacts,
    Func<EmergencyContact, NotificationContent> contentFor, NotificationMode mode, CancellationToken ct)
  {
    var allowed = mode == NotificationMode.AllowListOnly ? AllowedPhones() : new HashSet<string>();
    var deliveries = new List<AlertDelivery>();
    foreach (var c in contacts)
    {
      var content = contentFor(c);
      var real = mode switch { NotificationMode.Live => true, NotificationMode.AllowListOnly => allowed.Contains(c.Phone), _ => false };
      var smsText = smsOptions.Value.StripDiacritics ? TextHelper.StripDiacritics(content.Text) : content.Text;

      var smsResult = real ? await sms.SendAsync(c.Phone, smsText, ct) : DryRun(c, "sms");
      deliveries.Add(AlertDelivery.FromSend(alertId, c.Id, DeliveryChannel.Sms, smsResult));

      var token = c.LinkedUser?.FcmToken;
      if (string.IsNullOrEmpty(token)) continue;
      var pushResult = real ? await push.SendAsync(token, content.Title, content.Text, content.Data, ct) : DryRun(c, "push");
      deliveries.Add(AlertDelivery.FromSend(alertId, c.Id, DeliveryChannel.Push, pushResult));
    }

    var failed = deliveries.Count(d => d.Status == DeliveryStatus.Failed);
    if (failed > 0) log.LogWarning("Gửi thông báo cho alert {AlertId}: {Failed}/{Total} lượt thất bại", alertId, failed, deliveries.Count);
    return deliveries;
  }

  private Result<string> DryRun(EmergencyContact c, string channel)
  {
    log.LogInformation("[DRY-RUN] Bỏ qua gửi {Channel} tới {Phone}", channel, PhoneNumber.Mask(c.Phone));
    return DryRunMessageId;
  }

  private HashSet<string> AllowedPhones() => debugOptions.Value.AllowedPhones
    .Select(PhoneNumber.NormalizeVietnam).OfType<string>().ToHashSet();
}
