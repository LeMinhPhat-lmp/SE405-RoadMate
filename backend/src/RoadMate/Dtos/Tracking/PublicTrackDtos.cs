namespace RoadMate.Dtos.Tracking;

public record PublicLiveDto(double Lat, double Lng, double? Speed, int? Battery, bool NetworkOk, DateTime UpdatedAt);
public record PublicAlertDto(string Type, string Status, double Lat, double Lng, DateTimeOffset CreatedAt);
public record PublicTrackDto(string OwnerName, string Title, string Status, LocationDto Destination, PublicLiveDto? Live, List<PublicAlertDto> OpenAlerts);
public record PublicRouteDto(string? PlannedPolyline, List<LatLng> Points);
public record AckResultDto(string OwnerName, string AlertType, string AlertStatus, DateTime AckedAt);
