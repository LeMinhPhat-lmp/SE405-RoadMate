using RoadMate.Application.Shared;

namespace RoadMate.Application.Maps.DTOs.Responses;

/// <param name="Boundary">Polyline mã hoá ranh giới của đường (nếu Goong có).</param>
public record StreetResult(string Name, string Address, double Lat, double Lng, string? Commune, string? Province, string? PlaceId, string? Boundary);
