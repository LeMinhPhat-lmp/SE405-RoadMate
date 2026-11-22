using RoadMate.Application.Alerts;
using RoadMate.Application.Tracking;
using RoadMate.Domain.Alerts;
using RoadMate.Domain.Users;

namespace RoadMate.Application.Abstractions.Persistence;

public interface INotificationDeliveryRepository
{
  Task AddRangeAsync(IEnumerable<NotificationDelivery> deliveries, CancellationToken ct = default);
  Task<bool> ExistsAsync(Guid alertId, NotificationKind kind, int sequence, CancellationToken ct = default);
  /// <summary>Atomically claims due rows (Queued &amp; due, or Processing with an expired lease) with FOR UPDATE SKIP LOCKED; returns them tracked and already in Processing.</summary>
  Task<IReadOnlyList<NotificationDelivery>> ClaimDueAsync(string workerId, DateTimeOffset now, TimeSpan lease, int take, CancellationToken ct = default);
  Task<DeliveryContext?> LoadContextAsync(NotificationDelivery delivery, CancellationToken ct = default);
  Task<IReadOnlyList<NotificationDelivery>> ListByAlertAsync(Guid alertId, CancellationToken ct = default);
}
