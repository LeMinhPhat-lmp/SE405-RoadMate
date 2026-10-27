using RoadMate.Data;

namespace RoadMate.Repositories.Implements;

public class TripRepository(AppDbContext context, ICurrentUserContext currentUser)
  : SoftDeleteRepository<Trip>(context, currentUser), ITripRepository
{
  public Task<Trip?> GetOwnedAsync(Guid id, Guid userId, CancellationToken ct = default) =>
    Set.FirstOrDefaultAsync(t => t.Id == id && t.OwnerId == userId, ct);
  public Task<Trip?> GetOwnedWithDetailsAsync(Guid id, Guid userId, CancellationToken ct = default) =>
    Set.Include(t => t.Waypoints).Include(t => t.Recipients).Include(t => t.Live).Include(t => t.Stats)
      .AsSplitQuery().FirstOrDefaultAsync(t => t.Id == id && t.OwnerId == userId, ct);
  public Task<List<Trip>> ListByOwnerAsync(Guid userId, string? status, CancellationToken ct = default)
  {
    var q = Set.AsNoTracking().Where(t => t.OwnerId == userId);
    if (!string.IsNullOrEmpty(status)) q = q.Where(t => t.Status == status);
    return q.OrderByDescending(t => t.CreatedAt).ToListAsync(ct);
  }
  public Task<bool> HasActiveAsync(Guid userId, CancellationToken ct = default) =>
    Set.AnyAsync(t => t.OwnerId == userId && t.Status == TripStatus.Active, ct);
  public Task<Trip?> GetByTrackingTokenAsync(string token, CancellationToken ct = default) =>
    Set.Include(t => t.Owner).Include(t => t.Live).FirstOrDefaultAsync(t => t.TrackingToken == token, ct);
}
