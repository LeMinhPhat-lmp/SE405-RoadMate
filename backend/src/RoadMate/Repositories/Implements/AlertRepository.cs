using RoadMate.Data;
using RoadMate.Repositories.Interfaces;

namespace RoadMate.Repositories.Implements;

public class AlertRepository(AppDbContext context) : Repository<Alert>(context), IAlertRepository
{
  public Task<Alert?> GetByClientIdAsync(string clientId, CancellationToken ct = default) =>
      Set.Include(a => a.Deliveries).FirstOrDefaultAsync(a => a.ClientId == clientId, ct);
  public Task<Alert?> GetOwnedAsync(Guid id, Guid userId, CancellationToken ct = default) =>
      Set.Include(a => a.Trip).Include(a => a.User).FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId, ct);
  public Task<List<Alert>> ListByTripAsync(Guid tripId, CancellationToken ct = default) =>
      Set.AsNoTracking().Include(a => a.Deliveries).Where(a => a.TripId == tripId).OrderByDescending(a => a.CreatedAt).ToListAsync(ct);
  public Task<List<Alert>> ListOpenByTripAsync(Guid tripId, CancellationToken ct = default) =>
      Set.Where(a => a.TripId == tripId && (a.Status == AlertStatus.Pending || a.Status == AlertStatus.Sent)).ToListAsync(ct);
  public Task<int> CountByTripAsync(Guid tripId, CancellationToken ct = default) => Set.CountAsync(a => a.TripId == tripId, ct);
  public Task<List<Alert>> ListDueForRepeatAsync(DateTime now, int take, CancellationToken ct = default) =>
    Set.Include(a => a.Trip).Include(a => a.User)
      .Where(a => (a.Status == AlertStatus.Pending || a.Status == AlertStatus.Sent) && a.NextRepeatAt != null && a.NextRepeatAt <= now)
      .OrderBy(a => a.NextRepeatAt).Take(take).ToListAsync(ct);
}
