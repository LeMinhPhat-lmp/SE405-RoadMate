namespace RoadMate.Entities;

// P1: entity đã có để migration đầy đủ, CHƯA có service/endpoint (xem docs/SETUP_AND_LIMITATIONS.md).
public class Group : FullAuditedEntity
{
  public string Code { get; set; } = "";
  public Guid LeaderId { get; set; }
  public string Name { get; set; } = "";
  public string? MeetingName { get; set; }
  public double? MeetingLat { get; set; }
  public double? MeetingLng { get; set; }
  public DateTime? DepartAt { get; set; }
  public string Status { get; set; } = "open";
  public User? Leader { get; set; }
  public ICollection<GroupMember> Members { get; set; } = new List<GroupMember>();
  public ICollection<GroupMessage> Messages { get; set; } = new List<GroupMessage>();
  public ICollection<Trip> Trips { get; set; } = new List<Trip>();
}

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

public class GroupMessage : AuditableEntity                // CreatedAt = thời điểm gửi
{
  public Guid GroupId { get; set; }
  public Guid SenderId { get; set; }
  public string? Text { get; set; }
  public string? QuickType { get; set; }
  public Group? Group { get; set; }
  public User? Sender { get; set; }
}
