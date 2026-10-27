using RoadMate.Data;
using RoadMate.Repositories.Interfaces;

namespace RoadMate.Repositories.Implements;

public class TripRepository(AppDbContext context) : Repository<Trip>(context), ITripRepository
{
  public Task<Trip?> GetOwnedAsync(Guid id, string uid, CancellationToken ct = default) =>
    Set.FirstOrDefaultAsync(t => t.Id == id && t.OwnerId == uid, ct);
  public Task<Trip?> GetOwnedWithDetailsAsync(Guid id, string uid, CancellationToken ct = default) =>
    Set.Include(t => t.Waypoints).Include(t => t.Recipients).Include(t => t.Live).Include(t => t.Stats)
      .AsSplitQuery().FirstOrDefaultAsync(t => t.Id == id && t.OwnerId == uid, ct);
  public Task<List<Trip>> ListByOwnerAsync(string uid, string? status, CancellationToken ct = default)
  {
    var q = Set.AsNoTracking().Where(t => t.OwnerId == uid);
    if (!string.IsNullOrEmpty(status)) q = q.Where(t => t.Status == status);
    return q.OrderByDescending(t => t.CreatedAt).ToListAsync(ct);
  }
  public Task<bool> HasActiveAsync(string uid, CancellationToken ct = default) =>
    Set.AnyAsync(t => t.OwnerId == uid && t.Status == TripStatus.Active, ct);
  public Task<Trip?> GetByTrackingTokenAsync(string token, CancellationToken ct = default) =>
    Set.Include(t => t.Owner).Include(t => t.Live).FirstOrDefaultAsync(t => t.TrackingToken == token, ct);
}
