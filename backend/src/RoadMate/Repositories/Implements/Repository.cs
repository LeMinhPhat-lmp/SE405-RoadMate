using RoadMate.Data;

namespace RoadMate.Repositories.Implements;

/// <summary>
/// Repository chung. Lọc xoá mềm do global query filter trong AppDbContext đảm nhiệm,
/// nên mọi truy vấn (kể cả của repository con) đều tự ẩn bản ghi đã xoá mềm.
/// </summary>
public class Repository<T>(AppDbContext context) : IRepository<T> where T : BaseEntity
{
  protected readonly DbSet<T> Set = context.Set<T>();

  public async Task<T?> GetByIdAsync(Guid id, bool trackChanges = false, CancellationToken ct = default)
  {
    var query = Set.AsQueryable();
    if (!trackChanges) query = query.AsNoTracking();
    return await query.FirstOrDefaultAsync(entity => entity.Id == id, ct);
  }

  public async Task<IEnumerable<T>> ListAsync(int skip, int take, bool trackChanges = false, CancellationToken cancellationToken = default)
  {
    ArgumentOutOfRangeException.ThrowIfNegative(skip);
    ArgumentOutOfRangeException.ThrowIfNegativeOrZero(take);

    var query = Set.AsQueryable();
    if (!trackChanges) query = query.AsNoTracking();

    query = typeof(IAuditableEntity).IsAssignableFrom(typeof(T))
      ? query.OrderBy(entity => EF.Property<DateTimeOffset>(entity, nameof(IAuditableEntity.CreatedAt))).ThenBy(entity => entity.Id)
      : query.OrderBy(entity => entity.Id);
    return await query.Skip(skip).Take(take).ToListAsync(cancellationToken);
  }

  public async Task AddAsync(T entity, CancellationToken ct = default) => await Set.AddAsync(entity, ct);
  public Task AddRangeAsync(IEnumerable<T> entities, CancellationToken ct = default) => Set.AddRangeAsync(entities, ct);
  public void Update(T entity) => Set.Update(entity);
  public void Remove(T entity) => Set.Remove(entity);
  public void RemoveRange(IEnumerable<T> entities) => Set.RemoveRange(entities);
}
