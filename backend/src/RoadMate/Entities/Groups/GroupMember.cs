namespace RoadMate.Entities.Groups;

public class GroupMember : BaseEntity                      // unique (GroupId, UserId)
{
  public Guid GroupId { get; set; }
  public Guid UserId { get; set; }
  public string Role { get; set; } = "member";
  public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
  public double? LastLat { get; set; }
  public double? LastLng { get; set; }
  public int? LastBattery { get; set; }
  public DateTime? LocationUpdatedAt { get; set; }
  public Group? Group { get; set; }
  public User? User { get; set; }
}
