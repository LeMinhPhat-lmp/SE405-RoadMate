namespace RoadMate.Dtos.Trips;

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
