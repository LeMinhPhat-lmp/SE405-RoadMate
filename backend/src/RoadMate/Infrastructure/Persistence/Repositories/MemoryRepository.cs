using Microsoft.EntityFrameworkCore;
using RoadMate.Application.Abstractions.Identity;
using RoadMate.Application.Abstractions.Persistence;
using RoadMate.Application.Common;
using RoadMate.Domain.Trips;

namespace RoadMate.Infrastructure.Persistence.Repositories;

public class MemoryRepository(AppDbContext db, ICurrentUser user, TimeProvider clock) : SoftDeleteRepositoryBase<Memory>(db, user, clock), IMemoryRepository
{
  public Task<Memory?> GetOwnedAsync(Guid memoryId, Guid ownerId, CancellationToken ct = default) =>
    Set.FirstOrDefaultAsync(m => m.Id == memoryId && Db.Trips.Any(t => t.Id == m.TripId && t.OwnerId == ownerId), ct);

  public async Task<IReadOnlyList<Memory>> ListByTripAsync(Guid tripId, CancellationToken ct = default) =>
    await Set.AsNoTracking().Where(m => m.TripId == tripId).OrderBy(m => m.TakenAt).ThenBy(m => m.Id).ToListAsync(ct);

  public async Task<CursorPage<Memory>> ListByTripPageAsync(Guid tripId, CursorPosition? after, int limit, CancellationToken ct = default)
  {
    var take = CursorPagination.NormalizeLimit(limit);
    IQueryable<Memory> q = Set.AsNoTracking().Where(m => m.TripId == tripId);

    if (after is not null)
    {
      var afterAt = after.SortAt;
      var afterId = after.Id;
      q = q.Where(m => m.TakenAt > afterAt || (m.TakenAt == afterAt && m.Id.CompareTo(afterId) > 0));
    }

    var rows = await q.OrderBy(m => m.TakenAt).ThenBy(m => m.Id).Take(take + 1).ToListAsync(ct);
    return CursorPagination.ToPage(rows, take, m => new CursorPosition(m.TakenAt, m.Id));
  }
}
