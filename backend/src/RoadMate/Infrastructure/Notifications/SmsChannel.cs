using Microsoft.Extensions.Options;
using RoadMate.Application.Abstractions.Messaging;
using RoadMate.Application.Common;
using RoadMate.Domain.Alerts;
using RoadMate.Domain.Common;

namespace RoadMate.Infrastructure.Notifications;

public sealed class SmsChannel(ISmsProvider provider, IOptions<SmsOptions> options) : INotificationChannel
{
  public DeliveryChannel Channel => DeliveryChannel.Sms;

  public Task<Result<string>> SendAsync(NotificationMessage m, CancellationToken ct) =>
    provider.SendAsync(m.Recipient, options.Value.StripDiacritics ? TextHelper.StripDiacritics(m.Body) : m.Body, ct);
}
