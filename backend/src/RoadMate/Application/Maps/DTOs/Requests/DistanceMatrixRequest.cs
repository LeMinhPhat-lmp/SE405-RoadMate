using RoadMate.Application.Shared;

namespace RoadMate.Application.Maps.DTOs.Requests;

public record DistanceMatrixRequest(List<LatLng> Origins, List<LatLng> Destinations, string? Vehicle);
