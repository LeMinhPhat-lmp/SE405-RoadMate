using Microsoft.Extensions.Options;
using RoadMate.Application.Abstractions.Messaging;
using RoadMate.Application.Abstractions.Persistence;
using RoadMate.Application.Common;
using RoadMate.Application.Maps;
using RoadMate.Application.Tracking;
using RoadMate.Domain.Alerts;

namespace RoadMate.Application.Alerts.Interfaces;

public interface IDeliveryMessageBuilder
{
  /// <summary>Renders the message for one outbox row from the CURRENT alert state (latest location for repeats).</summary>
  Task<NotificationMessage?> BuildAsync(NotificationDelivery delivery, DeliveryContext ctx, CancellationToken ct);
}
