namespace RoadMate.Dtos.Maps;

public record DirectionsRequest(LatLng Origin, LatLng Destination, List<LatLng>? Waypoints, bool AvoidHighway, bool AvoidToll, string? Vehicle);
public record StepDto(string Instruction, string? Maneuver, int DistanceM, int DurationS);
public record DirectionsResponse(string Polyline, double DistanceKm, int EtaMinutes, List<StepDto> Steps);
public record PlacePrediction(string PlaceId, string Description, string? MainText, string? SecondaryText);
public record PlaceDetail(string PlaceId, string Name, string? Address, double Lat, double Lng);
public record ReverseResult(string? PlaceId, string Address);
