namespace RoadMate.Domain.Abstractions;

/// <summary>Entity + audit columns. Persistence fills CreatedAt/By and UpdatedAt/By on SaveChanges (setters are private).</summary>
public abstract class AuditableEntity : Entity, IAuditableEntity
{
  public DateTimeOffset CreatedAt { get; private set; }
  public DateTimeOffset? UpdatedAt { get; private set; }
  public Guid CreatedBy { get; private set; }
  public Guid? UpdatedBy { get; private set; }

  /// <summary>Marks the row as modified (bumps UpdatedAt) so optimistic-concurrency tokens change even when only children changed.</summary>
  public void Touch(DateTimeOffset now) => UpdatedAt = now;
}
