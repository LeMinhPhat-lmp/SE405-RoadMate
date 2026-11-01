namespace RoadMate.Dtos.Tracking;

public static class TelemetryMappers
{
  public static TripPoint ToEntity(this PointInput p, Guid tripId) => new()
  {
    TripId = tripId,
    Lat = p.Lat,
    Lng = p.Lng,
    Speed = p.Speed,
    Altitude = p.Altitude,
    Battery = p.Battery,
    RecordedAt = p.RecordedAt.ToUtc()
  };
}
