using RoadMate.Application.Alerts;
using RoadMate.Application.Tracking;
using RoadMate.Domain.Alerts;
using RoadMate.Domain.Users;

namespace RoadMate.Application.Abstractions.Persistence;

public interface IAlertAckRepository
{
  Task AddRangeAsync(IEnumerable<AlertAck> acks, CancellationToken ct = default);
  Task<IReadOnlyList<AlertAck>> ListByAlertAsync(Guid alertId, CancellationToken ct = default);
  /// <summary>Tracked ack with its Alert and the alert's owner (for the public ack page).</summary>
  Task<AlertAck?> GetByTokenAsync(string token, CancellationToken ct = default);
}
