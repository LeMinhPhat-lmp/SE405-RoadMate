namespace RoadMate.Services.Tracking;

public class TrackingLinkService(IUnitOfWork uow, IOptions<TrackingOptions> tracking) : ITrackingLinkService
{
  public async Task<Result<TrackingLinkDto>> CreateLinkAsync(Guid userId, Guid tripId, CancellationToken ct)
  {
    var trip = await uow.TripRepository.GetOwnedAsync(tripId, userId, ct);
    if (trip is null) return Errors.TripNotFound;
    if (trip.Status == TripStatus.Finished) return Error.Conflict("TRIP_FINISHED", "Chuyến đã kết thúc.");
    trip.RotateTrackingToken();   // tạo mới = đổi link, link cũ mất hiệu lực
    await uow.SaveChangesAsync(ct);
    return new TrackingLinkDto(TrackingUrl.Build(tracking.Value.BaseUrl, trip.TrackingToken)!, trip.TrackingToken!);
  }

  public async Task<Result> DisableLinkAsync(Guid userId, Guid tripId, CancellationToken ct)
  {
    var trip = await uow.TripRepository.GetOwnedAsync(tripId, userId, ct);
    if (trip is null) return Errors.TripNotFound;
    trip.DisableTracking();
    await uow.SaveChangesAsync(ct);
    return Result.Success();
  }
}
