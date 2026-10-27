using RoadMate.Data;
using RoadMate.Repositories.Interfaces;

namespace RoadMate.Repositories.Implements;

public class TripPointRepository(AppDbContext context) : Repository<TripPoint>(context), ITripPointRepository
{
  public Task<List<TripPoint>> ListByTripAsync(Guid tripId, CancellationToken ct = default) =>
    Set.AsNoTracking().Where(p => p.TripId == tripId).OrderBy(p => p.RecordedAt).ToListAsync(ct);
  public async Task<List<TripPoint>> ListRecentAsync(Guid tripId, int take, CancellationToken ct = default)
  {
    var l = await Set.AsNoTracking().Where(p => p.TripId == tripId).OrderByDescending(p => p.RecordedAt).Take(take).ToListAsync(ct);
    l.Reverse(); return l;
  }
}
