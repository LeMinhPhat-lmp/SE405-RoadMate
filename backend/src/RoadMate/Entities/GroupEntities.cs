namespace RoadMate.Entities;

// P1: đã có entity để migration đầy đủ, chưa làm endpoint.
public class Group
{
  public Guid Id { get; set; }
  public string Code { get; set; } = "";
  public string LeaderId { get; set; } = default!;
  public string Name { get; set; } = "";
  public string? MeetingName { get; set; }
  public double? MeetingLat { get; set; }
  public double? MeetingLng { get; set; }
  public DateTime? DepartAt { get; set; }
  public string Status { get; set; } = "open";
  public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
  public User? Leader { get; set; }
  public ICollection<GroupMember> Members { get; set; } = new List<GroupMember>();
  public ICollection<GroupMessage> Messages { get; set; } = new List<GroupMessage>();
  public ICollection<Trip> Trips { get; set; } = new List<Trip>();
}

public class GroupMember
{
  public Guid GroupId { get; set; }
  public string UserId { get; set; } = default!;
  public string Role { get; set; } = "member";
  public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
  public double? LastLat { get; set; }
  public double? LastLng { get; set; }
  public int? LastBattery { get; set; }
  public DateTime? LocationUpdatedAt { get; set; }
  public Group? Group { get; set; }
  public User? User { get; set; }
}

public class GroupMessage
{
  public Guid Id { get; set; }
  public Guid GroupId { get; set; }
  public string SenderId { get; set; } = default!;
  public string? Text { get; set; }
  public string? QuickType { get; set; }
  public DateTime SentAt { get; set; } = DateTime.UtcNow;
  public Group? Group { get; set; }
  public User? Sender { get; set; }
}
