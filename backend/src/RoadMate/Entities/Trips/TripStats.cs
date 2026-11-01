namespace RoadMate.Entities.Trips;

public class TripStats : BaseEntity                        // 1-1 với Trip (unique TripId)
{
  public Guid TripId { get; set; }
  public double DistanceKm { get; set; }
  public int MovingMin { get; set; }
  public int RestMin { get; set; }
  public int StopCount { get; set; }
  public double MaxSpeed { get; set; }
  public double AvgSpeed { get; set; }
  public double MaxAltitude { get; set; }
  public int AlertCount { get; set; }
  public Trip? Trip { get; set; }
}
