using RoadMate.Domain.Abstractions;

namespace RoadMate.Domain.Trips;

public sealed class TripWaypoint : Entity
{
  private TripWaypoint() { }
  public Guid TripId { get; private set; }
  public int Seq { get; private set; }
  public string Name { get; private set; } = "";
  public double Lat { get; private set; }
  public double Lng { get; private set; }
  public int? PlannedStayMin { get; private set; }

  internal static TripWaypoint Create(Guid tripId, int seq, WaypointSpec s) =>
    new() { Id = Guid.NewGuid(), TripId = tripId, Seq = seq, Name = s.Name, Lat = s.Point.Lat, Lng = s.Point.Lng, PlannedStayMin = s.PlannedStayMin };
}
