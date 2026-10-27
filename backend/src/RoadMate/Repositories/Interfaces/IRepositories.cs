using RoadMate.Entities.Abstractions;

namespace RoadMate.Repositories.Interfaces;

public interface IRepository<T> where T : BaseEntity
{
  Task<T?> GetByIdAsync(Guid id, bool trackChanges = false, CancellationToken ct = default);
  Task<IEnumerable<T>> ListAsync(int skip, int take, bool trackChanges = false, CancellationToken cancellationToken = default);
  Task AddAsync(T entity, CancellationToken ct = default);
  Task AddRangeAsync(IEnumerable<T> entities, CancellationToken ct = default);
  void Update(T entity);
  void Remove(T entity);
  void RemoveRange(IEnumerable<T> entities);
}
