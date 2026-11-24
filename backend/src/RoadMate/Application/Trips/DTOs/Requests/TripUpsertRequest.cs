using RoadMate.Application.Shared;
using RoadMate.Domain.Trips;

namespace RoadMate.Application.Trips.DTOs.Requests;

public record TripUpsertRequest(string Title, LocationResponse Origin, LocationResponse Destination, List<WaypointRequest>? Waypoints,
  string? PlannedPolyline, double? DistanceKm, int? EtaMinutes, bool AvoidHighway, bool AvoidToll, DateTime? ExpectedReturnTime,
  int? IdleMinutes, string? CrashSensitivity, bool LiveShare, List<Guid>? AlertContactIds)
{
  internal IReadOnlyList<WaypointSpec> ToSpecs() =>
    (Waypoints ?? []).Select(w => new WaypointSpec(w.Name, RoadMate.Domain.ValueObjects.GeoPoint.Create(w.Lat, w.Lng).Value, w.PlannedStayMin)).ToList();
}
