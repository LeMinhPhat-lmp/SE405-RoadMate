using Microsoft.EntityFrameworkCore;
using RoadMate.Application.Abstractions.Persistence;
using RoadMate.Domain.Alerts;

namespace RoadMate.Infrastructure.Persistence.Repositories;

public class AlertRepository(AppDbContext db) : RepositoryBase<Alert>(db), IAlertRepository
{
  public Task<Alert?> GetOwnedAsync(Guid alertId, Guid userId, CancellationToken ct = default) => Set.FirstOrDefaultAsync(a => a.Id == alertId && a.UserId == userId, ct);
  public Task<Alert?> GetByClientIdAsync(string clientId, CancellationToken ct = default) => Set.FirstOrDefaultAsync(a => a.ClientId == clientId, ct);

  public async Task<IReadOnlyList<Alert>> ListOpenByTripAsync(Guid tripId, AlertType? type, CancellationToken ct = default) =>
    await Set.Where(a => a.TripId == tripId && (a.Status == AlertStatus.Pending || a.Status == AlertStatus.Sent) && (type == null || a.Type == type)).ToListAsync(ct);

  public Task<bool> HasOpenAsync(Guid tripId, AlertType type, CancellationToken ct = default) =>
    Set.AnyAsync(a => a.TripId == tripId && a.Type == type && (a.Status == AlertStatus.Pending || a.Status == AlertStatus.Sent), ct);

  public Task<bool> ExistsSinceAsync(Guid tripId, AlertType type, DateTimeOffset since, CancellationToken ct = default) =>
    Set.AnyAsync(a => a.TripId == tripId && a.Type == type && a.CreatedAt >= since, ct);

  public Task<int> CountForTripAsync(Guid tripId, CancellationToken ct = default) => Set.CountAsync(a => a.TripId == tripId, ct);

  /// <summary>Lease due alerts so concurrent workers (several instances) never repeat the same alert; the lease expires if a worker dies.</summary>
  public async Task<IReadOnlyList<Guid>> ClaimDueForRepeatAsync(string workerId, DateTimeOffset now, TimeSpan lease, int take, CancellationToken ct = default) =>
    await Db.Database.SqlQuery<Guid>($"""
      UPDATE "Alerts" SET "LockedBy" = {workerId}, "LockedUntil" = {(now + lease).UtcDateTime}
      WHERE "Id" IN (
        SELECT "Id" FROM "Alerts"
        WHERE "Status" IN ('pending','sent') AND "NextRepeatAt" IS NOT NULL AND "NextRepeatAt" <= {now.UtcDateTime}
          AND ("LockedUntil" IS NULL OR "LockedUntil" < {now.UtcDateTime})
        ORDER BY "NextRepeatAt" LIMIT {take} FOR UPDATE SKIP LOCKED)
      RETURNING "Id" AS "Value"
      """).ToListAsync(ct);
}
