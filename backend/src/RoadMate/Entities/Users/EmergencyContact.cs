namespace RoadMate.Entities.Users;

public class EmergencyContact : FullAuditedEntity
{
  public const int MaxPerUser = 5;

  public Guid UserId { get; set; }
  public Guid? LinkedUserId { get; set; }
  public string Name { get; set; } = "";
  public string Phone { get; set; } = "";                  // +84...
  public string? Relation { get; set; }
  public int Priority { get; set; }

  public User? User { get; set; }
  public User? LinkedUser { get; set; }
}
