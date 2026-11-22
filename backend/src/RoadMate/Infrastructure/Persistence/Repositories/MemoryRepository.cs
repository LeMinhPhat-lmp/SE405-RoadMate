using Microsoft.EntityFrameworkCore;
using RoadMate.Application.Abstractions.Identity;
using RoadMate.Application.Abstractions.Persistence;
using RoadMate.Domain.Trips;

namespace RoadMate.Infrastructure.Persistence.Repositories;

public class MemoryRepository(AppDbContext db, ICurrentUser user, TimeProvider clock) : SoftDeleteRepositoryBase<Memory>(db, user, clock), IMemoryRepository
{
  public Task<Memory?> GetOwnedAsync(Guid memoryId, Guid ownerId, CancellationToken ct = default) =>
    Set.FirstOrDefaultAsync(m => m.Id == memoryId && Db.Trips.Any(t => t.Id == m.TripId && t.OwnerId == ownerId), ct);

  public async Task<IReadOnlyList<Memory>> ListByTripAsync(Guid tripId, CancellationToken ct = default) =>
    await Set.AsNoTracking().Where(m => m.TripId == tripId).OrderBy(m => m.TakenAt).ToListAsync(ct);
}
