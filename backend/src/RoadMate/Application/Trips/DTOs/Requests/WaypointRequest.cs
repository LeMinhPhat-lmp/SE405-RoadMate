using RoadMate.Application.Shared;

namespace RoadMate.Application.Trips.DTOs.Requests;

public record WaypointRequest(string Name, double Lat, double Lng, int? PlannedStayMin);
