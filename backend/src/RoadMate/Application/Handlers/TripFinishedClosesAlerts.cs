using RoadMate.Application.Abstractions.Messaging;
using RoadMate.Application.Abstractions.Persistence;
using RoadMate.Application.Alerts;
using RoadMate.Domain.Alerts;
using RoadMate.Domain.Events;

namespace RoadMate.Application.Handlers;

/// <summary>Finishing a trip means the owner is safe: close every open alert of the trip (resolvedBy = user).</summary>
public sealed class TripFinishedClosesAlerts(IAlertRepository alerts) : IDomainEventHandler<TripFinished>
{
  public async Task HandleAsync(TripFinished e, CancellationToken ct)
  {
    foreach (var a in await alerts.ListOpenByTripAsync(e.TripId, null, ct)) a.Close(AlertStatus.Resolved, AlertResolver.User, e.OccurredAt);
  }
}
