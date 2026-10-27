namespace RoadMate.Entities.Abstractions;

/// <summary>BaseEntity + audit + soft delete. DeletedAt/By do SoftDeleteRepository gán.</summary>
public abstract class FullAuditedEntity : AuditableEntity, ISoftDeletableEntity
{
  public DateTimeOffset? DeletedAt { get; private set; }
  public Guid? DeletedBy { get; private set; }
}
