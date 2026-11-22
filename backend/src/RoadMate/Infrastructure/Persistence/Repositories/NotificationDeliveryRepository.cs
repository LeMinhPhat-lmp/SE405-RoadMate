using Microsoft.EntityFrameworkCore;
using RoadMate.Application.Abstractions.Persistence;
using RoadMate.Domain.Alerts;

namespace RoadMate.Infrastructure.Persistence.Repositories;

public class NotificationDeliveryRepository(AppDbContext db) : INotificationDeliveryRepository
{
  public Task AddRangeAsync(IEnumerable<NotificationDelivery> deliveries, CancellationToken ct = default) => db.NotificationDeliveries.AddRangeAsync(deliveries, ct);

  public Task<bool> ExistsAsync(Guid alertId, NotificationKind kind, int sequence, CancellationToken ct = default) =>
    db.NotificationDeliveries.AnyAsync(d => d.AlertId == alertId && d.Kind == kind && d.Sequence == sequence, ct);

  /// <summary>
  /// Same transition as <c>NotificationDelivery.Claim</c>, done atomically in SQL with FOR UPDATE SKIP LOCKED so several workers never take the same row.
  /// A row left in Processing by a crashed worker becomes claimable again once its lease expired.
  /// </summary>
  public async Task<IReadOnlyList<NotificationDelivery>> ClaimDueAsync(string workerId, DateTimeOffset now, TimeSpan lease, int take, CancellationToken ct = default)
  {
    var ids = await db.Database.SqlQuery<Guid>($"""
      UPDATE "NotificationDeliveries" SET "Status"='processing', "Attempts"="Attempts"+1, "LockedBy"={workerId}, "LockedUntil"={(now + lease).UtcDateTime}
      WHERE "Id" IN (
        SELECT "Id" FROM "NotificationDeliveries"
        WHERE ("Status"='queued' AND "NextAttemptAt" <= {now.UtcDateTime}) OR ("Status"='processing' AND "LockedUntil" < {now.UtcDateTime})
        ORDER BY "NextAttemptAt" LIMIT {take} FOR UPDATE SKIP LOCKED)
      RETURNING "Id" AS "Value"
      """).ToListAsync(ct);
    return ids.Count == 0 ? [] : await db.NotificationDeliveries.Where(d => ids.Contains(d.Id)).ToListAsync(ct);
  }

  public async Task<DeliveryContext?> LoadContextAsync(NotificationDelivery d, CancellationToken ct = default)
  {
    var alert = await db.Alerts.FirstOrDefaultAsync(a => a.Id == d.AlertId, ct);
    if (alert is null) return null;
    var owner = await db.Users.FirstOrDefaultAsync(u => u.Id == alert.UserId, ct);
    if (owner is null) return null;
    var contact = d.ContactId is { } cid ? await db.EmergencyContacts.AsNoTracking().FirstOrDefaultAsync(c => c.Id == cid, ct) : null;
    var token = d.ContactId is { } cid2 ? await db.AlertAcks.AsNoTracking().Where(a => a.AlertId == alert.Id && a.ContactId == cid2).Select(a => a.Token).FirstOrDefaultAsync(ct) : null;
    var push = contact?.LinkedUserId is { } lid ? await db.Users.AsNoTracking().Where(u => u.Id == lid).Select(u => u.FcmToken).FirstOrDefaultAsync(ct) : null;
    return new DeliveryContext(alert, owner, d.ContactId, contact?.Phone, token, push);
  }

  public async Task<IReadOnlyList<NotificationDelivery>> ListByAlertAsync(Guid alertId, CancellationToken ct = default) =>
    await db.NotificationDeliveries.AsNoTracking().Where(d => d.AlertId == alertId).ToListAsync(ct);
}
