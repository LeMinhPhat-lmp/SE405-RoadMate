using RoadMate.Application.Shared;

namespace RoadMate.Application.Tracking.DTOs.Responses;

public record PublicRouteResponse(string? PlannedPolyline, List<LatLng> Points);
