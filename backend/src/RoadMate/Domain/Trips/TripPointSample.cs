namespace RoadMate.Domain.Trips;

public sealed record TripPointSample(double Lat, double Lng, double? Speed, double? Altitude, int? Battery, DateTimeOffset RecordedAt);
