using RoadMate.Application.Shared;

namespace RoadMate.Application.Maps.DTOs.Responses;

// ---------- Geocoding
public record ReverseResult(string? PlaceId, string Address, string? Name = null, double? Lat = null, double? Lng = null,
  string? Commune = null, string? Province = null, List<string>? Types = null, string? DeprecatedAddress = null);
