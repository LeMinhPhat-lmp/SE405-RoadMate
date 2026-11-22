using RoadMate.Domain.Abstractions;

namespace RoadMate.Application.Abstractions.Persistence;

/// <summary>Write-model port: aggregates are returned TRACKED; saving is the UnitOfWork's job.</summary>
public interface IRepository<T> where T : Entity
{
  Task<T?> GetByIdAsync(Guid id, CancellationToken ct = default);
  Task AddAsync(T entity, CancellationToken ct = default);
}
