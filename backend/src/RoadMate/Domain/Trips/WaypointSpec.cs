using RoadMate.Domain.ValueObjects;

namespace RoadMate.Domain.Trips;

public sealed record WaypointSpec(string Name, GeoPoint Point, int? PlannedStayMin);
