using RoadMate.Application.Abstractions.Persistence;
using RoadMate.Application.Common;
using RoadMate.Domain.Alerts;
using RoadMate.Domain.Common;
using RoadMate.Domain.ValueObjects;

namespace RoadMate.Application.Alerts.Interfaces;

/// <summary>Single place where alerts are created (user, simulator, server monitors): idempotency, policy, outbox planning, one commit.</summary>
public interface IAlertRaiser
{
  Task<Result<RaisedAlert>> RaiseAsync(RaiseAlertCommand cmd, DateTimeOffset now, CancellationToken ct);
}
