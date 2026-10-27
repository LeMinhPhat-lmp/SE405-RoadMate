namespace RoadMate.Dtos;

public record LatLng(double Lat, double Lng);
public record LocationDto(string Name, double Lat, double Lng);

// ---- Auth / profile
public record SyncRequest(string? FcmToken, string? Name);
public record UpdateProfileRequest(string? Name, string? AvatarUrl, string? BloodType, string? MedicalNotes, string? VehicleType,
  string? PlateNumber, int? IdleMinutes, string? CrashSensitivity, string? Language, string? Theme, string? FcmToken);
public record UserDto(Guid Id, string Name, string? Email, string? Phone, string? AvatarUrl, string? BloodType, string? MedicalNotes,
  string? VehicleType, string? PlateNumber, int IdleMinutes, string CrashSensitivity, string Language, string Theme);

// ---- Contacts
public record ContactRequest(string Name, string Phone, string? Relation, int? Priority);
public record ContactDto(Guid Id, string Name, string Phone, string? Relation, int Priority, bool HasApp);
public record DeliveryDto(Guid? ContactId, string Channel, string Status, string? Error);

// ---- Maps
public record DirectionsRequest(LatLng Origin, LatLng Destination, List<LatLng>? Waypoints, bool AvoidHighway, bool AvoidToll, string? Vehicle);
public record StepDto(string Instruction, string? Maneuver, int DistanceM, int DurationS);
public record DirectionsResponse(string Polyline, double DistanceKm, int EtaMinutes, List<StepDto> Steps);
public record PlacePrediction(string PlaceId, string Description, string? MainText, string? SecondaryText);
public record PlaceDetail(string PlaceId, string Name, string? Address, double Lat, double Lng);
public record ReverseResult(string? PlaceId, string Address);

// ---- Trips
public record WaypointInput(string Name, double Lat, double Lng, int? PlannedStayMin);
public record WaypointDto(int Seq, string Name, double Lat, double Lng, int? PlannedStayMin);
public record TripUpsertRequest(string Title, LocationDto Origin, LocationDto Destination, List<WaypointInput>? Waypoints,
  string? PlannedPolyline, double? DistanceKm, int? EtaMinutes, bool AvoidHighway, bool AvoidToll, DateTime? ExpectedReturnTime,
  int? IdleMinutes, string? CrashSensitivity, bool LiveShare, List<Guid>? AlertContactIds);
public record TripDto(Guid Id, string Title, string Status, LocationDto Origin, LocationDto Destination, List<WaypointDto> Waypoints,
  string? PlannedPolyline, double DistanceKm, int EtaMinutes, bool AvoidHighway, bool AvoidToll, DateTime? StartTime, DateTime? EndTime,
  DateTime? ExpectedReturnTime, int? IdleMinutes, string? CrashSensitivity, bool LiveShare, string? TrackingUrl, DateTime? RestUntil,
  List<Guid> AlertContactIds);
public record TripListItemDto(Guid Id, string Title, string Status, string OriginName, string DestName, double DistanceKm,
  DateTime? StartTime, DateTime? EndTime, DateTimeOffset CreatedAt);
public record StartTripRequest(DateTime? ExpectedReturnTime, List<Guid>? AlertContactIds, bool? LiveShare);
public record StartTripResponse(TripDto Trip, string? TrackingUrl, List<string> Warnings);
public record TripStatsDto(double DistanceKm, int MovingMin, int RestMin, int StopCount, double MaxSpeed, double AvgSpeed, double MaxAltitude, int AlertCount);
public record TripSummaryDto(TripDto Trip, TripStatsDto Stats);
public record RestRequest(int Minutes);
public record PointInput(double Lat, double Lng, double? Speed, double? Altitude, int? Battery, DateTime RecordedAt);
public record AddPointsRequest(List<PointInput> Points);
public record HeartbeatRequest(double Lat, double Lng, double? Speed, int? Battery, bool NetworkOk);
public record TrackingLinkDto(string Url, string Token);

// ---- Memories / journal
public record MemoryRequest(string Type, string? MediaUrl, string? Text, double? Lat, double? Lng, string? Mood, string? PlaceName, int? Rating, DateTime? TakenAt);
public record MemoryDto(Guid Id, string Type, string? MediaUrl, string? Text, double? Lat, double? Lng, string? Mood, string? PlaceName, int? Rating, DateTime TakenAt);
public record JournalDayDto(DateOnly Date, List<MemoryDto> Entries);
public record JournalDto(Guid TripId, string Title, string Status, TripStatsDto? Stats, List<WaypointDto> Stops, List<JournalDayDto> Days);

// ---- Alerts
public record CreateAlertRequest(string ClientId, string Type, double Lat, double Lng, int? Battery);
public record AlertLocationRequest(double Lat, double Lng, int? Battery);
public record AlertDto(Guid Id, Guid TripId, string Type, string Status, double Lat, double Lng, int? Battery, int RepeatCount,
    DateTimeOffset CreatedAt, DateTime? ResolvedAt, string? ResolvedBy, List<DeliveryDto>? Deliveries);
public record SimulateAlertRequest(Guid TripId, string? Type, double? Lat, double? Lng);

// ---- Public tracking
public record PublicLiveDto(double Lat, double Lng, double? Speed, int? Battery, bool NetworkOk, DateTime UpdatedAt);
public record PublicAlertDto(string Type, string Status, double Lat, double Lng, DateTimeOffset CreatedAt);
public record PublicTrackDto(string OwnerName, string Title, string Status, LocationDto Destination, PublicLiveDto? Live, List<PublicAlertDto> OpenAlerts);
public record PublicRouteDto(string? PlannedPolyline, List<LatLng> Points);
