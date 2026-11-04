namespace RoadMate.Entities.Groups;

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
