using RoadMate.Application.Shared;

namespace RoadMate.Application.Maps.DTOs.Responses;

public record NearbyPlace(string PlaceId, string Name, string? Address, double Lat, double Lng, int DistanceM, List<string>? Types = null);
