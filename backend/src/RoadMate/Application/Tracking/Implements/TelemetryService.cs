using RoadMate.Application.Abstractions.Persistence;
using RoadMate.Application.Common;
using RoadMate.Application.Tracking.DTOs.Requests;
using RoadMate.Application.Tracking.DTOs.Responses;
using RoadMate.Application.Tracking.Interfaces;
using RoadMate.Application.Trips;
using RoadMate.Domain.Common;
using RoadMate.Domain.Trips;
using RoadMate.Domain.ValueObjects;

namespace RoadMate.Application.Tracking.Implements;

/// <summary>
/// Hot path (every few seconds per rider): set-based writes, no aggregate graph. "Latest position wins" is enforced atomically in SQL;
/// when the position advanced the trip raises <c>LocationReported</c> (handlers close lost-signal alerts and update the group member).
/// </summary>
public class TelemetryService(ITripRepository trips, ITripTelemetryWriter writer, IUnitOfWork uow, IGroupReadModel groups, TimeProvider clock) : ITelemetryService
{
  public async Task<Result> AddPointsAsync(Guid userId, Guid tripId, AddPointsRequest req, CancellationToken ct)
  {
    if (req.Points.Count == 0) return Result.Success();
    var found = await trips.GetOwnedActiveAsync(tripId, userId, false, ct);
    if (found.IsFailure) return found.Error;

    var samples = req.Points.Select(p => new TripPointSample(p.Lat, p.Lng, p.Speed, p.Altitude, p.Battery, p.RecordedAt.ToUtcOffset())).OrderBy(s => s.RecordedAt).ToList();
    await writer.AppendPointsAsync(tripId, samples, ct);
    var last = samples[^1];
    return await ReportAsync(found.Value, new TripLiveUpdate(tripId, GeoPoint.Create(last.Lat, last.Lng).Value, last.Speed, last.Battery, true, last.RecordedAt, clock.GetUtcNow()), ct);
  }

  public async Task<Result<HeartbeatResponse>> HeartbeatAsync(Guid userId, Guid tripId, HeartbeatRequest req, CancellationToken ct)
  {
    var found = await trips.GetOwnedActiveAsync(tripId, userId, false, ct);
    if (found.IsFailure) return found.Error;
    var now = clock.GetUtcNow();
    var reported = await ReportAsync(found.Value, new TripLiveUpdate(tripId, GeoPoint.Create(req.Lat, req.Lng).Value, req.Speed, req.Battery, req.NetworkOk, now, now), ct);
    if (reported.IsFailure) return reported.Error;
    var group = found.Value.GroupId is { } gid ? await groups.GetSnapshotAsync(gid, userId, req.MessagesSince.ToUtcOffset(), 50, ct) : null;
    return new HeartbeatResponse(group);
  }

  private async Task<Result> ReportAsync(Trip trip, TripLiveUpdate update, CancellationToken ct)
  {
    if (!await writer.UpsertLiveIfNewerAsync(update, ct)) return Result.Success();
    trip.ReportLocation(update.Location, update.Battery, update.ReceivedAt);
    await uow.SaveChangesAsync(ct);                       // LocationReported → lost-signal closing + group position (same transaction)
    return Result.Success();
  }
}
