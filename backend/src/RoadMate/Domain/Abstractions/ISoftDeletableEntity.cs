namespace RoadMate.Domain.Abstractions;

public interface ISoftDeletableEntity
{
  DateTimeOffset? DeletedAt { get; }
  Guid? DeletedBy { get; }
  void SoftDelete(Guid by, DateTimeOffset at);
  void Restore();
}
