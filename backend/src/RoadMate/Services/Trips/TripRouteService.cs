namespace RoadMate.Services.Trips;

public class TripRouteService(IUnitOfWork uow, IRoutingService routing) : ITripRouteService
{
  public async Task<Result<TripRouteResponse>> RefreshAsync(Guid userId, Guid tripId, RefreshRouteRequest req, CancellationToken ct)
  {
    var trip = await uow.TripRepository.GetOwnedWithDetailsAsync(tripId, userId, ct);
    if (trip is null) return Errors.TripNotFound;
    if (trip.Status == TripStatus.Finished) return Error.Conflict("TRIP_FINISHED", "Chuyến đã kết thúc, không thể tính lại lộ trình.");

    var reroute = req.From is not null;
    if (reroute && trip.Status != TripStatus.Active) return Errors.TripNotActive;
    if (reroute && !Geo.ValidLatLng(req.From!.Lat, req.From.Lng)) return Errors.InvalidLocation;

    var ordered = trip.Waypoints.OrderBy(w => w.Seq).ToList();
    var passed = req.PassedWaypoints ?? 0;
    if (passed < 0 || passed > ordered.Count) return Error.Validation("INVALID_PASSED_WAYPOINTS", $"passedWaypoints phải từ 0 đến {ordered.Count}.");

    var origin = reroute ? req.From! : new LatLng(trip.OriginLat, trip.OriginLng);
    var remaining = (reroute ? ordered.Skip(passed) : ordered).Select(w => new LatLng(w.Lat, w.Lng)).ToList();
    var route = await routing.DirectionsAsync(
      new DirectionsRequest(origin, new LatLng(trip.DestLat, trip.DestLng), remaining, trip.AvoidHighway, trip.AvoidToll, req.Vehicle), ct);
    if (route.IsFailure) return route.Error;

    trip.PlannedPolyline = route.Value.Polyline;
    if (!reroute)
    {
      trip.DistanceKm = route.Value.DistanceKm;
      trip.EtaMinutes = route.Value.EtaMinutes;
    }
    await uow.SaveChangesAsync(ct);
    return new TripRouteResponse(trip.Id, reroute, route.Value);
  }
}
