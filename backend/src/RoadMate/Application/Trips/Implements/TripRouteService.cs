using RoadMate.Application.Abstractions.Persistence;
using RoadMate.Application.Maps;
using RoadMate.Application.Maps.DTOs.Requests;
using RoadMate.Application.Maps.DTOs.Responses;
using RoadMate.Application.Maps.Interfaces;
using RoadMate.Application.Shared;
using RoadMate.Application.Trips.Interfaces;
using RoadMate.Domain.Common;
using RoadMate.Domain.Trips;

namespace RoadMate.Application.Trips.Implements;

public class TripRouteService(ITripRepository trips, IRoutingService routing, IUnitOfWork uow) : ITripRouteService
{
  public async Task<Result<TripRouteResponse>> RefreshAsync(Guid userId, Guid tripId, RefreshRouteRequest req, CancellationToken ct)
  {
    var trip = await trips.GetOwnedWithDetailsAsync(tripId, userId, ct);
    if (trip is null) return Errors.TripNotFound;
    if (trip.Status == TripStatus.Finished) return Errors.TripFinished;
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
    trip.SetRoute(route.Value.Polyline, reroute ? null : route.Value.DistanceKm, reroute ? null : route.Value.EtaMinutes);
    await uow.SaveChangesAsync(ct);
    return new TripRouteResponse(trip.Id, reroute, route.Value);
  }
}
