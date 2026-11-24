namespace RoadMate.Domain.Abstractions;

/// <summary>Auditable + soft delete (data owned by a user, hidden by a global query filter instead of removed).</summary>
public abstract class FullAuditedEntity : AuditableEntity, ISoftDeletableEntity
{
  public DateTimeOffset? DeletedAt { get; private set; }
  public Guid? DeletedBy { get; private set; }
  public bool IsDeleted => DeletedAt is not null;

  public void SoftDelete(Guid by, DateTimeOffset at) { DeletedAt = at; DeletedBy = by; }
  public void Restore() { DeletedAt = null; DeletedBy = null; }
}
