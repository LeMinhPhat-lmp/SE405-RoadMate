using RoadMate.Domain.Alerts;

namespace RoadMate.Application.Abstractions.Persistence;

public interface IAlertRepository : IGenericRepository<Alert>
{
  /// <summary>Tracked alert of this user.</summary>
  Task<Alert?> GetOwnedAsync(Guid alertId, Guid userId, CancellationToken ct = default);
  Task<Alert?> GetByClientIdAsync(string clientId, CancellationToken ct = default);
  /// <summary>Tracked open (pending/sent) alerts of a trip, optionally of one type.</summary>
  Task<IReadOnlyList<Alert>> ListOpenByTripAsync(Guid tripId, AlertType? type, CancellationToken ct = default);
  Task<bool> HasOpenAsync(Guid tripId, AlertType type, CancellationToken ct = default);
  Task<bool> ExistsSinceAsync(Guid tripId, AlertType type, DateTimeOffset since, CancellationToken ct = default);
  /// <summary>Atomically leases open alerts whose NextRepeatAt is due (FOR UPDATE SKIP LOCKED); other workers cannot take them until the lease expires.</summary>
  Task<IReadOnlyList<Guid>> ClaimDueForRepeatAsync(string workerId, DateTimeOffset now, TimeSpan lease, int take, CancellationToken ct = default);
  Task<int> CountForTripAsync(Guid tripId, CancellationToken ct = default);
}
