using Microsoft.EntityFrameworkCore;
using RoadMate.Application.Abstractions.Persistence;
using RoadMate.Domain.Abstractions;

namespace RoadMate.Infrastructure.Persistence.Repositories;

/// <summary>Shared EF Core repository mechanics. Reads default to no tracking; committing is <see cref="IUnitOfWork"/>'s job.</summary>
public abstract class RepositoryBase<T>(AppDbContext db) : IGenericRepository<T> where T : Entity
{
  protected AppDbContext Db { get; } = db;
  protected DbSet<T> Set => Db.Set<T>();

  protected IQueryable<T> Query(bool trackChanges) => trackChanges ? Set : Set.AsNoTracking();

  public Task<T?> GetByIdAsync(Guid entityId, bool trackChanges = false, CancellationToken cancellationToken = default) =>
    Query(trackChanges).FirstOrDefaultAsync(e => e.Id == entityId, cancellationToken);

  public Task<List<T>> ListEntitiesAsync(int? skip, int? take, bool trackChanges = false, CancellationToken cancellationToken = default)
  {
    IQueryable<T> query = Query(trackChanges).OrderBy(e => e.Id);
    if (skip is > 0) query = query.Skip(skip.Value);
    if (take is > 0) query = query.Take(take.Value);
    return query.ToListAsync(cancellationToken);
  }

  public async Task AddAsync(T entity, CancellationToken cancellationToken = default) => await Set.AddAsync(entity, cancellationToken);

  public Task AddRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default) => Set.AddRangeAsync(entities, cancellationToken);

  public void Update(T entity) => Set.Update(entity);

  public void Remove(T entity) => Set.Remove(entity);

  public void RemoveRange(IEnumerable<T> entities) => Set.RemoveRange(entities);
}
