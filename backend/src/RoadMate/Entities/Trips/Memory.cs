namespace RoadMate.Entities.Trips;

public class Memory : FullAuditedEntity
{
  public Guid TripId { get; set; }
  public string Type { get; set; } = MemoryType.Note;
  public string? MediaUrl { get; set; }                    // URL Firebase Storage (client tự upload)
  public string? Text { get; set; }
  public double? Lat { get; set; }
  public double? Lng { get; set; }
  public string? Mood { get; set; }
  public string? PlaceName { get; set; }
  public int? Rating { get; set; }
  public DateTime TakenAt { get; set; }
  public Trip? Trip { get; set; }
}
