namespace RoadMate.Entities.Abstractions;

/// <summary>BaseEntity + audit. CreatedAt/By, UpdatedAt/By do AppDbContext tự gán khi SaveChanges (setter private).</summary>
public abstract class AuditableEntity : BaseEntity, IAuditableEntity
{
  public DateTimeOffset CreatedAt { get; private set; }
  public DateTimeOffset? UpdatedAt { get; private set; }
  public Guid CreatedBy { get; private set; }
  public Guid? UpdatedBy { get; private set; }
}
