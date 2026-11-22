using Microsoft.Extensions.Options;
using RoadMate.Application.Abstractions.Persistence;
using RoadMate.Application.Common;
using RoadMate.Application.Tracking.DTOs.Responses;
using RoadMate.Application.Tracking.Interfaces;
using RoadMate.Domain.Common;

namespace RoadMate.Application.Tracking.Implements;

public class TrackingLinkService(ITripRepository trips, IUnitOfWork uow, IOptions<TrackingOptions> tracking) : ITrackingLinkService
{
  public async Task<Result<TrackingLinkResponse>> CreateLinkAsync(Guid userId, Guid tripId, CancellationToken ct)
  {
    var trip = await trips.GetOwnedAsync(tripId, userId, ct);
    if (trip is null) return Errors.TripNotFound;
    var rotated = trip.RotateTrackingToken();      // a new token replaces the old one: the old link dies
    if (rotated.IsFailure) return rotated.Error;
    await uow.SaveChangesAsync(ct);
    return new TrackingLinkResponse(TrackingUrl.Build(tracking.Value.BaseUrl, trip.TrackingToken)!, trip.TrackingToken!);
  }

  public async Task<Result> DisableLinkAsync(Guid userId, Guid tripId, CancellationToken ct)
  {
    var trip = await trips.GetOwnedAsync(tripId, userId, ct);
    if (trip is null) return Errors.TripNotFound;
    trip.DisableTracking();
    await uow.SaveChangesAsync(ct);
    return Result.Success();
  }
}
