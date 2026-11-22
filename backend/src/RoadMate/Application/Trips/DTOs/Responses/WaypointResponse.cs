using RoadMate.Application.Shared;

namespace RoadMate.Application.Trips.DTOs.Responses;

public record WaypointResponse(int Seq, string Name, double Lat, double Lng, int? PlannedStayMin);
