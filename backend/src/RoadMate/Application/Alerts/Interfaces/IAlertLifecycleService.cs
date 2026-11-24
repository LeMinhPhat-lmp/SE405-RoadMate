using RoadMate.Application.Alerts.DTOs.Requests;
using RoadMate.Domain.Common;

namespace RoadMate.Application.Alerts.Interfaces;

public interface IAlertLifecycleService
{
  Task<Result> UpdateLocationAsync(Guid userId, Guid alertId, AlertLocationRequest req, CancellationToken ct);
  /// <summary>"Tôi ổn" (false alarm).</summary>
  Task<Result> CancelAsync(Guid userId, Guid alertId, CancellationToken ct);
  /// <summary>Confirmed safe.</summary>
  Task<Result> ResolveAsync(Guid userId, Guid alertId, CancellationToken ct);
}
