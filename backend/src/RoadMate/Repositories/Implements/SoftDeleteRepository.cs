using RoadMate.Data;

namespace RoadMate.Repositories.Implements;

/// <summary>
/// Repository cho entity có ISoftDeletableEntity. DeletedAt/DeletedBy có setter private nên được gán qua ChangeTracker;
/// AppDbContext.SaveChanges sẽ tự cập nhật UpdatedAt/UpdatedBy cho thao tác này.
/// </summary>
public abstract class SoftDeleteRepository<T>(AppDbContext context, ICurrentUserContext currentUser)
  : Repository<T>(context), ISoftDeleteRepository<T>
  where T : BaseEntity, ISoftDeletableEntity
{
  public void Delete(T entity) => Mark(entity, DateTimeOffset.UtcNow, currentUser.UserId ?? AuditConstants.SystemUserId);
  public void DeleteRange(IEnumerable<T> entities) { foreach (var e in entities) Delete(e); }
  public void Restore(T entity) => Mark(entity, null, null);
  public void RestoreRange(IEnumerable<T> entities) { foreach (var e in entities) Restore(e); }

  public async Task<T?> GetDeletedByIdAsync(Guid id, bool trackChanges = true, CancellationToken ct = default)
  {
    var query = Set.IgnoreQueryFilters().Where(e => e.Id == id && EF.Property<DateTimeOffset?>(e, nameof(ISoftDeletableEntity.DeletedAt)) != null);
    if (!trackChanges) query = query.AsNoTracking();
    return await query.FirstOrDefaultAsync(ct);
  }

  public async Task<IEnumerable<T>> ListDeletedAsync(int skip, int take, CancellationToken ct = default)
  {
    ArgumentOutOfRangeException.ThrowIfNegative(skip);
    ArgumentOutOfRangeException.ThrowIfNegativeOrZero(take);
    return await Set.IgnoreQueryFilters().AsNoTracking()
      .Where(e => EF.Property<DateTimeOffset?>(e, nameof(ISoftDeletableEntity.DeletedAt)) != null)
      .OrderBy(e => e.Id).Skip(skip).Take(take).ToListAsync(ct);
  }

  private void Mark(T entity, DateTimeOffset? at, Guid? by)
  {
    var entry = context.Entry(entity);
    entry.Property(nameof(ISoftDeletableEntity.DeletedAt)).CurrentValue = at;
    entry.Property(nameof(ISoftDeletableEntity.DeletedBy)).CurrentValue = by;
  }
}
