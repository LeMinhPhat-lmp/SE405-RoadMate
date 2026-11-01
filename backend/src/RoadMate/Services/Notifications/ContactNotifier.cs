using RoadMate.Infrastructure.Notifications;

namespace RoadMate.Services.Notifications;

public class ContactNotifier(ISmsSender sms, IPushSender push, IOptions<SmsOptions> options) : IContactNotifier
{
  public async Task<List<AlertDelivery>> NotifyAsync(Guid alertId, IReadOnlyList<EmergencyContact> contacts, NotificationContent content, CancellationToken ct)
  {
    var smsText = options.Value.StripDiacritics ? TextHelper.StripDiacritics(content.Text) : content.Text;
    var deliveries = new List<AlertDelivery>();
    foreach (var c in contacts)
    {
      var smsResult = await sms.SendAsync(c.Phone, smsText, ct);
      deliveries.Add(AlertDelivery.FromSend(alertId, c.Id, DeliveryChannel.Sms, smsResult));
      if (string.IsNullOrEmpty(c.LinkedUser?.FcmToken)) continue;
      var pushResult = await push.SendAsync(c.LinkedUser!.FcmToken!, content.Title, content.Text, content.Data, ct);
      deliveries.Add(AlertDelivery.FromSend(alertId, c.Id, DeliveryChannel.Push, pushResult));
    }
    return deliveries;
  }
}
