namespace RoadMate.Services.Interfaces;

public interface IAlertService
{
  Task<Result<AlertDto>> CreateAsync(Guid userId, Guid tripId, CreateAlertRequest req, CancellationToken ct);
  Task<Result<List<AlertDto>>> ListAsync(Guid userId, Guid tripId, CancellationToken ct);
  Task<Result<AlertDto>> UpdateLocationAsync(Guid userId, Guid alertId, AlertLocationRequest req, CancellationToken ct);
  Task<Result<AlertDto>> CancelAsync(Guid userId, Guid alertId, CancellationToken ct);
  Task<Result<AlertDto>> ResolveAsync(Guid userId, Guid alertId, CancellationToken ct);
  Task ProcessDueRepeatsAsync(CancellationToken ct);
}
