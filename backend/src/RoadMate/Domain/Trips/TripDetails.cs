using RoadMate.Domain.Users;
using RoadMate.Domain.ValueObjects;

namespace RoadMate.Domain.Trips;

public sealed record TripDetails(string Title, string OriginName, GeoPoint Origin, string DestName, GeoPoint Destination, string? PlannedPolyline,
  double DistanceKm, int EtaMinutes, bool AvoidHighway, bool AvoidToll, DateTimeOffset? ExpectedReturnTime, int? IdleMinutes,
  CrashSensitivity? CrashSensitivity, bool LiveShare);
