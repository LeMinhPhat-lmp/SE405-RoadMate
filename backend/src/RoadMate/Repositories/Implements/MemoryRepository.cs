using RoadMate.Data;
using RoadMate.Repositories.Interfaces;

namespace RoadMate.Repositories.Implements;

public class MemoryRepository(AppDbContext context) : Repository<Memory>(context), IMemoryRepository
{
  public Task<List<Memory>> ListByTripAsync(Guid tripId, CancellationToken ct = default) =>
      Set.AsNoTracking().Where(m => m.TripId == tripId).OrderBy(m => m.TakenAt).ToListAsync(ct);
  public Task<Memory?> GetOwnedAsync(Guid id, string uid, CancellationToken ct = default) =>
      Set.Include(m => m.Trip).FirstOrDefaultAsync(m => m.Id == id && m.Trip!.OwnerId == uid, ct);
}
