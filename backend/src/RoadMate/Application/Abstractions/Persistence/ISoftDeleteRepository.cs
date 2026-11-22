using RoadMate.Domain.Abstractions;

namespace RoadMate.Application.Abstractions.Persistence;

/// <summary>Soft delete only; hard removal is not part of any public port.</summary>
public interface ISoftDeleteRepository<T> where T : Entity, ISoftDeletableEntity
{
  void Delete(T entity);
}
