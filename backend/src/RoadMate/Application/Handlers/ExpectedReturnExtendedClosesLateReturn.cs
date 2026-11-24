using RoadMate.Application.Abstractions.Messaging;
using RoadMate.Application.Abstractions.Persistence;
using RoadMate.Application.Alerts;
using RoadMate.Domain.Alerts;
using RoadMate.Domain.Events;

namespace RoadMate.Application.Handlers;

/// <summary>The owner answered "still riding": the late-return alert is no longer valid.</summary>
public sealed class ExpectedReturnExtendedClosesLateReturn(IAlertRepository alerts) : IDomainEventHandler<ExpectedReturnExtended>
{
  public async Task HandleAsync(ExpectedReturnExtended e, CancellationToken ct)
  {
    foreach (var a in await alerts.ListOpenByTripAsync(e.TripId, AlertType.LateReturn, ct)) a.Close(AlertStatus.Resolved, AlertResolver.User, e.OccurredAt);
  }
}
