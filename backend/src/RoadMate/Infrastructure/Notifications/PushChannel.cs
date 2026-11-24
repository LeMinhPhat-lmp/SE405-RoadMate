using RoadMate.Application.Abstractions.Messaging;
using RoadMate.Domain.Alerts;
using RoadMate.Domain.Common;

namespace RoadMate.Infrastructure.Notifications;

public sealed class PushChannel(IPushProvider provider) : INotificationChannel
{
  public DeliveryChannel Channel => DeliveryChannel.Push;

  public Task<Result<string>> SendAsync(NotificationMessage m, CancellationToken ct) =>
    provider.SendAsync(m.Recipient, m.Title, m.Body, new Dictionary<string, string>(m.Data), ct);
}
