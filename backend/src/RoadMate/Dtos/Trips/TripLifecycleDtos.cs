namespace RoadMate.Dtos.Trips;

public record StartTripRequest(DateTime? ExpectedReturnTime, List<Guid>? AlertContactIds, bool? LiveShare);
public record StartTripResponse(TripDto Trip, string? TrackingUrl, List<string> Warnings);
public record TripStatsDto(double DistanceKm, int MovingMin, int RestMin, int StopCount, double MaxSpeed, double AvgSpeed, double MaxAltitude, int AlertCount);
public record TripSummaryDto(TripDto Trip, TripStatsDto Stats);
public record RestRequest(int Minutes);
