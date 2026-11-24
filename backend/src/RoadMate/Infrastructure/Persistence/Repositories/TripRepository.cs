using Microsoft.EntityFrameworkCore;
using RoadMate.Application.Abstractions.Identity;
using RoadMate.Application.Abstractions.Persistence;
using RoadMate.Application.Common;
using RoadMate.Domain.Trips;

namespace RoadMate.Infrastructure.Persistence.Repositories;

public class TripRepository(AppDbContext db, ICurrentUser user, TimeProvider clock) : SoftDeleteRepositoryBase<Trip>(db, user, clock), ITripRepository
{
  public Task<Trip?> GetOwnedAsync(Guid tripId, Guid ownerId, CancellationToken ct = default) =>
    Set.FirstOrDefaultAsync(t => t.Id == tripId && t.OwnerId == ownerId, ct);

  public Task<Trip?> GetOwnedWithDetailsAsync(Guid tripId, Guid ownerId, CancellationToken ct = default) =>
    Set.Include(t => t.Waypoints).Include(t => t.Recipients).Include(t => t.Live).Include(t => t.Stats).AsSplitQuery()
      .FirstOrDefaultAsync(t => t.Id == tripId && t.OwnerId == ownerId, ct);

  public async Task<IReadOnlyList<Trip>> ListByOwnerAsync(Guid ownerId, TripStatus? status, CancellationToken ct = default) =>
    await Set.AsNoTracking().Include(t => t.Waypoints).Include(t => t.Recipients).AsSplitQuery()
      .Where(t => t.OwnerId == ownerId && (status == null || t.Status == status))
      .OrderByDescending(t => t.CreatedAt).ThenByDescending(t => t.Id).ToListAsync(ct);

  public async Task<CursorPage<Trip>> ListByOwnerPageAsync(Guid ownerId, TripStatus? status, CursorPosition? after, int limit, CancellationToken ct = default)
  {
    var take = CursorPagination.NormalizeLimit(limit);
    IQueryable<Trip> q = Set.AsNoTracking()
      .Include(t => t.Waypoints).Include(t => t.Recipients).AsSplitQuery()
      .Where(t => t.OwnerId == ownerId && (status == null || t.Status == status));

    if (after is not null)
    {
      var afterAt = after.SortAt;
      var afterId = after.Id;
      q = q.Where(t => t.CreatedAt < afterAt || (t.CreatedAt == afterAt && t.Id.CompareTo(afterId) < 0));
    }

    var rows = await q.OrderByDescending(t => t.CreatedAt).ThenByDescending(t => t.Id).Take(take + 1).ToListAsync(ct);
    return CursorPagination.ToPage(rows, take, t => new CursorPosition(t.CreatedAt, t.Id));
  }

  public Task<bool> HasActiveAsync(Guid ownerId, CancellationToken ct = default) => Set.AnyAsync(t => t.OwnerId == ownerId && t.Status == TripStatus.Active, ct);

  public async Task<IReadOnlyList<Trip>> ListActiveForMonitoringAsync(CancellationToken ct = default) =>
    await Set.Include(t => t.Live).Include(t => t.Waypoints).Include(t => t.Owner).AsSplitQuery().Where(t => t.Status == TripStatus.Active).ToListAsync(ct);

  public async Task<IReadOnlyList<Trip>> ListInGroupAsync(Guid userId, Guid groupId, CancellationToken ct = default) =>
    await Set.Where(t => t.OwnerId == userId && t.GroupId == groupId).ToListAsync(ct);
}
