using RoadMate.Domain.Alerts;

namespace RoadMate.Application.Abstractions.Persistence;

public interface IAlertAckRepository : IGenericRepository<AlertAck>
{
  /// <summary>No tracking, all ack tokens for an alert.</summary>
  Task<IReadOnlyList<AlertAck>> ListByAlertAsync(Guid alertId, CancellationToken ct = default);
  /// <summary>Tracked ack with its Alert and the alert's owner (for the public ack page).</summary>
  Task<AlertAck?> GetByTokenAsync(string token, CancellationToken ct = default);
}
