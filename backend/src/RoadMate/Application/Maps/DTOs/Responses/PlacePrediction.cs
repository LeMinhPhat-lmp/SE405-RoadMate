using RoadMate.Application.Shared;

namespace RoadMate.Application.Maps.DTOs.Responses;

// ---------- Places
public record PlacePrediction(string PlaceId, string Description, string? MainText, string? SecondaryText,
  bool HasChildren = false, List<string>? Types = null, string? Commune = null, string? Province = null,
  double? DistanceMeters = null, string? DeprecatedDescription = null);
