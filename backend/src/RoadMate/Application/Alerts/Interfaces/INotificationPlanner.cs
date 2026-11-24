using Microsoft.Extensions.Options;
using RoadMate.Application.Abstractions.Persistence;
using RoadMate.Domain.Alerts;
using RoadMate.Domain.Common;

namespace RoadMate.Application.Alerts.Interfaces;

public interface INotificationPlanner
{
  /// <summary>Adds outbox rows (and missing per-contact ack tokens) for one alert event. Idempotent per (alert, kind, sequence). Nothing is sent here.</summary>
  Task<PlanResult> PlanAsync(Alert alert, NotificationKind kind, int sequence, DateTimeOffset now, CancellationToken ct);
  /// <summary>True when there is at least one recipient and every recipient already acknowledged (repeating is pointless).</summary>
  Task<bool> AllRecipientsAckedAsync(Alert alert, CancellationToken ct);
}
