using RoadMate.Data;

namespace RoadMate.Repositories.Implements;

public class MemoryRepository(AppDbContext context, ICurrentUserContext currentUser)
  : SoftDeleteRepository<Memory>(context, currentUser), IMemoryRepository
{
  public Task<List<Memory>> ListByTripAsync(Guid tripId, CancellationToken ct = default) =>
    Set.AsNoTracking().Where(m => m.TripId == tripId).OrderBy(m => m.TakenAt).ToListAsync(ct);
  public Task<Memory?> GetOwnedAsync(Guid id, Guid userId, CancellationToken ct = default) =>
    Set.Include(m => m.Trip).FirstOrDefaultAsync(m => m.Id == id && m.Trip!.OwnerId == userId, ct);
}
