namespace RoadMate.Entities.Trips;

public class TripLive : BaseEntity                         // 1-1 với Trip (unique TripId), luôn ghi đè
{
  public Guid TripId { get; set; }
  public double Lat { get; set; }
  public double Lng { get; set; }
  public double? Speed { get; set; }
  public int? Battery { get; set; }
  public bool NetworkOk { get; set; } = true;
  public DateTime UpdatedAt { get; set; }
  public Trip? Trip { get; set; }
}
