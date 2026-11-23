using RoadMate.Application.Abstractions.Messaging;
using RoadMate.Application.Abstractions.Persistence;
using RoadMate.Application.Alerts;
using RoadMate.Domain.Alerts;
using RoadMate.Domain.Events;

namespace RoadMate.Application.Handlers;

/// <summary>Data arrived again: auto-close open lost-signal alerts (resolvedBy = system) and clear the trip flag.</summary>
public sealed class LocationReportedClosesLostSignal(IAlertRepository alerts, ITripRepository trips) : IDomainEventHandler<LocationReported>
{
  public async Task HandleAsync(LocationReported e, CancellationToken ct)
  {
    if (!e.ClearsLostSignal) return;
    foreach (var a in await alerts.ListOpenByTripAsync(e.TripId, AlertType.LostSignal, ct)) a.Close(AlertStatus.Resolved, AlertResolver.System, e.OccurredAt);
    (await trips.GetByIdAsync(e.TripId, trackChanges: true, ct))?.MarkSignalRestored();
  }
}
