namespace RoadMate.Services.Tracking;

public class PublicTrackingService(IUnitOfWork uow) : IPublicTrackingService
{
  public async Task<Result<PublicTrackDto>> GetAsync(string token, CancellationToken ct)
  {
    var trip = await uow.TripRepository.GetByActiveTrackingTokenAsync(token, DateTime.UtcNow, ct);
    if (trip is null) return Errors.LinkInvalid;
    var open = (await uow.AlertRepository.ListOpenByTripAsync(trip.Id, ct))
        .Select(a => new PublicAlertDto(a.Type, a.Status, a.Lat, a.Lng, a.CreatedAt)).ToList();
    var live = trip.Live is null ? null : new PublicLiveDto(trip.Live.Lat, trip.Live.Lng, trip.Live.Speed, trip.Live.Battery, trip.Live.NetworkOk, trip.Live.UpdatedAt);
    return new PublicTrackDto(trip.Owner?.Name ?? "", trip.Title, trip.Status, new LocationDto(trip.DestName, trip.DestLat, trip.DestLng), live, open);
  }

  public async Task<Result<PublicRouteDto>> GetRouteAsync(string token, CancellationToken ct)
  {
    var trip = await uow.TripRepository.GetByActiveTrackingTokenAsync(token, DateTime.UtcNow, ct);
    if (trip is null) return Errors.LinkInvalid;
    var pts = await uow.TripPointRepository.ListRecentAsync(trip.Id, 2000, ct);
    return new PublicRouteDto(trip.PlannedPolyline, pts.Select(p => new LatLng(p.Lat, p.Lng)).ToList());
  }

  public async Task<Result> AckAsync(string token, CancellationToken ct)
  {
    var trip = await uow.TripRepository.GetByActiveTrackingTokenAsync(token, DateTime.UtcNow, ct);
    if (trip is null) return Errors.LinkInvalid;
    var now = DateTime.UtcNow;
    foreach (var a in await uow.AlertRepository.ListOpenByTripAsync(trip.Id, ct)) a.Close(AlertStatus.Resolved, AlertResolver.Contact, now);
    await uow.SaveChangesAsync(ct);
    return Result.Success();
  }
}
