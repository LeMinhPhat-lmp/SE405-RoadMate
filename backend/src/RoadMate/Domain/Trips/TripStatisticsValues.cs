namespace RoadMate.Domain.Trips;

public sealed record TripStatisticsValues(double DistanceKm, int MovingMin, int RestMin, int StopCount, double MaxSpeed, double AvgSpeed, double MaxAltitude, int AlertCount);
