namespace RoadMate.Entities.Trips;

public class TripAlertRecipient : BaseEntity               // bảng nối, unique (TripId, ContactId)
{
  public Guid TripId { get; set; }
  public Guid ContactId { get; set; }
  public Trip? Trip { get; set; }
  public EmergencyContact? Contact { get; set; }
}
