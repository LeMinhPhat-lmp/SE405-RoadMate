using Microsoft.EntityFrameworkCore;
using RoadMate.Domain.Abstractions;

namespace RoadMate.Infrastructure.Persistence.Repositories;

/// <summary>Shared mechanics. Aggregates are returned TRACKED; repositories never call SaveChanges.</summary>
public abstract class RepositoryBase<T>(AppDbContext db) where T : Entity
{
  protected AppDbContext Db { get; } = db;
  protected DbSet<T> Set => Db.Set<T>();

  public Task<T?> GetByIdAsync(Guid id, CancellationToken ct = default) => Set.FirstOrDefaultAsync(e => e.Id == id, ct);

  public async Task AddAsync(T entity, CancellationToken ct = default) => await Set.AddAsync(entity, ct);
}
