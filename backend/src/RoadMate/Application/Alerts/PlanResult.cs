using Microsoft.Extensions.Options;
using RoadMate.Application.Abstractions.Persistence;
using RoadMate.Domain.Alerts;
using RoadMate.Domain.Common;

namespace RoadMate.Application.Alerts;

public sealed record PlanResult(IReadOnlyList<NotificationDelivery> Deliveries, IReadOnlyList<AlertAck> Acks)
{
  public static readonly PlanResult Empty = new([], []);
}
