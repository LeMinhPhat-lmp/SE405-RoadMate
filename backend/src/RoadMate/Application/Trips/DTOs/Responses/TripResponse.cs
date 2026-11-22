using RoadMate.Application.Shared;

namespace RoadMate.Application.Trips.DTOs.Responses;

public record TripResponse(Guid Id, string Title, string Status, LocationResponse Origin, LocationResponse Destination, List<WaypointResponse> Waypoints,
  string? PlannedPolyline, double DistanceKm, int EtaMinutes, bool AvoidHighway, bool AvoidToll, DateTime? StartTime, DateTime? EndTime,
  DateTime? ExpectedReturnTime, int? IdleMinutes, string? CrashSensitivity, bool LiveShare, string? TrackingUrl, DateTime? RestUntil,
  List<Guid> AlertContactIds, Guid? GroupId);
