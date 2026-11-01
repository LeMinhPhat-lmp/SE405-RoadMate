namespace RoadMate.Entities.Trips;

public class TripWaypoint : BaseEntity
{
  public Guid TripId { get; set; }
  public int Seq { get; set; }
  public string Name { get; set; } = "";
  public double Lat { get; set; }
  public double Lng { get; set; }
  public int? PlannedStayMin { get; set; }
  public Trip? Trip { get; set; }
}
