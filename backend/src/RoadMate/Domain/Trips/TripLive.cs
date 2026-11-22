using RoadMate.Domain.Abstractions;

namespace RoadMate.Domain.Trips;

/// <summary>1–1 with Trip, always overwritten. ReportedAt = device time of the newest accepted position; UpdatedAt = server receive time (lost-signal clock).</summary>
public sealed class TripLive : Entity
{
  private TripLive()
  {
    NetworkOk = true;
  }

  public Guid TripId { get; private set; }
  public double Lat { get; private set; }
  public double Lng { get; private set; }
  public double? Speed { get; private set; }
  public int? Battery { get; private set; }
  public bool NetworkOk { get; private set; }
  public DateTimeOffset ReportedAt { get; private set; }
  public DateTimeOffset UpdatedAt { get; private set; }

  internal static TripLive CreateFor(Guid tripId) =>
    new() { Id = Guid.NewGuid(), TripId = tripId };

  internal void Apply(double lat, double lng, double? speed, int? battery, bool networkOk, DateTimeOffset reportedAt, DateTimeOffset receivedAt)
  {
    Lat = lat;
    Lng = lng;
    Speed = speed;
    Battery = battery ?? Battery;
    NetworkOk = networkOk;
    ReportedAt = reportedAt;
    UpdatedAt = receivedAt;
  }

  internal void Refresh(DateTimeOffset receivedAt)
  {
    UpdatedAt = receivedAt;
  }
}
