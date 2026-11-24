using RoadMate.Domain.Abstractions;

namespace RoadMate.Domain.Trips;

/// <summary>Recomputed periodically and on TripFinished; cached in its own table to serve list queries cheaply.</summary>
public sealed class TripStats : Entity
{
  private TripStats() { }

  public Guid TripId { get; private set; }
  public double DistanceKm { get; private set; }
  public int MovingMin { get; private set; }
  public int RestMin { get; private set; }
  public int StopCount { get; private set; }
  public double MaxSpeed { get; private set; }
  public double AvgSpeed { get; private set; }
  public double MaxAltitude { get; private set; }
  public int AlertCount { get; private set; }

  internal static TripStats CreateFor(Guid tripId) =>
    new() { Id = Guid.NewGuid(), TripId = tripId };

  internal void Set(TripStatisticsValues v)
  {
    DistanceKm = v.DistanceKm;
    MovingMin = v.MovingMin;
    RestMin = v.RestMin;
    StopCount = v.StopCount;
    MaxSpeed = v.MaxSpeed;
    AvgSpeed = v.AvgSpeed;
    MaxAltitude = v.MaxAltitude;
    AlertCount = v.AlertCount;
  }
}
