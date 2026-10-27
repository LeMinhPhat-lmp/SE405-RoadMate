namespace RoadMate.Entities.Abstractions;

public interface IAuditableEntity
{
  DateTimeOffset CreatedAt { get; }
  DateTimeOffset? UpdatedAt { get; }
  Guid CreatedBy { get; }
  Guid? UpdatedBy { get; }
}