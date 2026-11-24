using RoadMate.Domain.Abstractions;

namespace RoadMate.Domain.Trips;

/// <summary>GPS sample row (high volume: written in bulk by <c>ITripPointWriter</c>, no audit/soft delete).</summary>
public sealed class TripPoint : Entity
{
  private TripPoint() { }
  public Guid TripId { get; private set; }
  public double Lat { get; private set; }
  public double Lng { get; private set; }
  public double? Speed { get; private set; }
  public double? Altitude { get; private set; }
  public int? Battery { get; private set; }
  public DateTimeOffset RecordedAt { get; private set; }
  public static TripPoint Create(Guid tripId, TripPointSample s) =>
    new() { Id = Guid.NewGuid(), TripId = tripId, Lat = s.Lat, Lng = s.Lng, Speed = s.Speed, Altitude = s.Altitude, Battery = s.Battery, RecordedAt = s.RecordedAt };
}
