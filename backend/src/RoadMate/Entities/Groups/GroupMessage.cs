namespace RoadMate.Entities.Groups;

public class GroupMessage : AuditableEntity                // CreatedAt = thời điểm gửi
{
  public Guid GroupId { get; set; }
  public Guid SenderId { get; set; }
  public string? Text { get; set; }
  public string? QuickType { get; set; }
  public Group? Group { get; set; }
  public User? Sender { get; set; }
}
