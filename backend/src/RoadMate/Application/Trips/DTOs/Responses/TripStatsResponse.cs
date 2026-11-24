namespace RoadMate.Application.Trips.DTOs.Responses;

public record TripStatsResponse(double DistanceKm, int MovingMin, int RestMin, int StopCount, double MaxSpeed, double AvgSpeed, double MaxAltitude, int AlertCount);
