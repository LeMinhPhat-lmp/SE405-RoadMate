using RoadMate.Domain.Abstractions;

namespace RoadMate.Application.Abstractions.Persistence;

/// <summary>Logical deletion contract. Marks entities as soft-deleted with audit metadata instead of physically removing rows.</summary>
public interface ISoftDeleteRepository<T> where T : Entity, ISoftDeletableEntity
{
  void Delete(T entity);
  void DeleteRange(IEnumerable<T> entities);
}
