namespace RoadMate.Entities.Abstractions;

public interface ISoftDeletableEntity
{
  bool IsDeleted => DeletedAt is not null;
  DateTimeOffset? DeletedAt { get; }
  Guid? DeletedBy { get; }
}