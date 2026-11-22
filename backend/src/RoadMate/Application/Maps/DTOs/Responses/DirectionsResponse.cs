using RoadMate.Application.Shared;

namespace RoadMate.Application.Maps.DTOs.Responses;

public record DirectionsResponse(string Polyline, double DistanceKm, int EtaMinutes, List<StepResponse> Steps,
  List<LegResponse> Legs, BoundsResponse? Bounds, List<RouteWarningResponse> Warnings);
