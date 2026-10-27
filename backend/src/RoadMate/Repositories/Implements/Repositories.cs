using RoadMate.Data;
using RoadMate.Entities.Abstractions;
using RoadMate.Repositories.Interfaces;

namespace RoadMate.Repositories;

public class Repository<T>(AppDbContext context) : IRepository<T> where T : BaseEntity
{
  protected readonly DbSet<T> Set = context.Set<T>();
  public async Task<T?> GetByIdAsync(Guid id, bool trackChanges = false, CancellationToken ct = default)
  {
    var query = Set.AsQueryable();
    if (!trackChanges)
      query = query.AsNoTracking();
    if (typeof(ISoftDeletableEntity).IsAssignableFrom(typeof(T)))
      query = query.Where(entity => EF.Property<DateTimeOffset?>(entity, nameof(ISoftDeletableEntity.DeletedAt)) == null);
    return await query.FirstOrDefaultAsync(entity => entity.Id == id, ct);
  }
  public async Task<IEnumerable<T>> ListAsync(int skip, int take, bool trackChanges = false, CancellationToken cancellationToken = default)
  {
    ArgumentOutOfRangeException.ThrowIfNegative(skip);
    ArgumentOutOfRangeException.ThrowIfNegativeOrZero(take);

    var query = Set.AsQueryable();
    if (!trackChanges)
      query = query.AsNoTracking();

    if (typeof(ISoftDeletableEntity).IsAssignableFrom(typeof(T)))
      query = query.Where(entity => EF.Property<DateTimeOffset?>(entity, nameof(ISoftDeletableEntity.DeletedAt)) == null);

    if (typeof(IAuditableEntity).IsAssignableFrom(typeof(T)))
      query = query
        .OrderBy(entity => EF.Property<DateTimeOffset>(entity, nameof(IAuditableEntity.CreatedAt)))
        .ThenBy(entity => entity.Id);
    else
      query = query.OrderBy(entity => entity.Id);
    return await query
      .Skip(skip)
      .Take(take)
      .ToListAsync(cancellationToken);
  }
  public async Task AddAsync(T entity, CancellationToken ct = default) => await Set.AddAsync(entity, ct);
  public Task AddRangeAsync(IEnumerable<T> entities, CancellationToken ct = default) => Set.AddRangeAsync(entities, ct);
  public void Update(T entity) => Set.Update(entity);
  public void Remove(T entity) => Set.Remove(entity);
  public void RemoveRange(IEnumerable<T> entities) => Set.RemoveRange(entities);
}

public class UnitOfWork(
  AppDbContext context,
  IUserRepository userRepository,
  IEmergencyContactRepository emergencyContactRepository,
  ITripRepository tripRepository,
  ITripPointRepository tripPointRepository,
  IMemoryRepository memoryRepository,
  IAlertRepository alertRepository,
  IAlertDeliveryRepository alertDeliveryRepository
) : IUnitOfWork
{
  public IUserRepository UserRepository => userRepository;
  public IEmergencyContactRepository EmergencyContactRepository => emergencyContactRepository;
  public ITripRepository TripRepository => tripRepository;
  public ITripPointRepository TripPointRepository => tripPointRepository;
  public IMemoryRepository MemoryRepository => memoryRepository;
  public IAlertRepository AlertRepository => alertRepository;
  public IAlertDeliveryRepository AlertDeliveryRepository => alertDeliveryRepository;

  public Task<int> SaveChangesAsync(CancellationToken ct = default) => context.SaveChangesAsync(ct);

  public async Task ExecuteInTransactionAsync(Func<CancellationToken, Task> action, CancellationToken ct = default)
  {
    await using var tx = await context.Database.BeginTransactionAsync(ct);
    try { await action(ct); await tx.CommitAsync(ct); }
    catch { await tx.RollbackAsync(ct); throw; }
  }
  public void Dispose() { }   // DbContext do DI quản lý
}
