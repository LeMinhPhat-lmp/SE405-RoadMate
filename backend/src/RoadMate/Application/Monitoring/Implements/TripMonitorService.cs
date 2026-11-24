using Microsoft.Extensions.Options;
using RoadMate.Application.Abstractions.Messaging;
using RoadMate.Application.Abstractions.Persistence;
using RoadMate.Application.Alerts;
using RoadMate.Application.Monitoring.Interfaces;
using RoadMate.Domain.Alerts;
using RoadMate.Domain.Common;
using RoadMate.Domain.Trips;
using RoadMate.Domain.ValueObjects;

namespace RoadMate.Application.Monitoring.Implements;

/// <summary>
/// Server-side detection of the two conditions only the server can notice: silence (lost signal) and not coming home (late return).
/// Each trip is checked in its OWN scope so a failing trip never blocks the others.
/// </summary>
public class TripMonitorService(ITripRepository trips, IIsolatedExecutor isolated, ILogger<TripMonitorService> log) : ITripMonitorService
{
  public async Task RunOnceAsync(CancellationToken ct)
  {
    foreach (var tripId in (await trips.ListActiveForMonitoringAsync(ct)).Select(t => t.Id).ToList())
    {
      try { await isolated.RunAsync<ITripCheckService>(s => s.CheckAsync(tripId, ct), ct); }
      catch (Exception ex) when (ex is not OperationCanceledException) { log.LogError(ex, "Giám sát chuyến {TripId} lỗi, bỏ qua và tiếp tục chuyến khác", tripId); }
    }
  }
}
