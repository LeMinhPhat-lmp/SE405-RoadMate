using RoadMate.Application.Shared;

namespace RoadMate.Application.Maps.DTOs.Responses;

public record GeocodeResult(string PlaceId, string Name, string Address, double Lat, double Lng,
  string? Commune = null, string? Province = null, List<string>? Types = null, string? DeprecatedAddress = null);
