namespace RoadMate.Entities.Trips;

public class TripPoint : BaseEntity                        // số lượng lớn: không audit, không soft delete
{
  public Guid TripId { get; set; }
  public double Lat { get; set; }
  public double Lng { get; set; }
  public double? Speed { get; set; }                       // km/h
  public double? Altitude { get; set; }
  public int? Battery { get; set; }
  public DateTime RecordedAt { get; set; }
  public Trip? Trip { get; set; }
}
