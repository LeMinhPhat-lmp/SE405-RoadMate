using RoadMate.Application.Abstractions.Persistence;
using RoadMate.Application.Alerts.DTOs.Requests;
using RoadMate.Domain.Alerts;
using RoadMate.Domain.Common;
using RoadMate.Domain.ValueObjects;
using RoadMate.Application.Alerts.Interfaces;

namespace RoadMate.Application.Alerts.Implements;

public class AlertLifecycleService(IAlertRepository alerts, IUnitOfWork uow, TimeProvider clock) : IAlertLifecycleService
{
  public async Task<Result> UpdateLocationAsync(Guid userId, Guid alertId, AlertLocationRequest req, CancellationToken ct)
  {
    var alert = await alerts.GetOwnedAsync(alertId, userId, ct);
    if (alert is null) return Errors.AlertNotFound;
    var at = GeoPoint.Create(req.Lat, req.Lng);
    if (at.IsFailure) return at.Error;
    var r = alert.UpdateLocation(at.Value, req.Battery);
    if (r.IsFailure) return r.Error;
    await uow.SaveChangesAsync(ct);
    return Result.Success();
  }

  public Task<Result> CancelAsync(Guid userId, Guid alertId, CancellationToken ct) => CloseAsync(userId, alertId, AlertStatus.Cancelled, ct);
  public Task<Result> ResolveAsync(Guid userId, Guid alertId, CancellationToken ct) => CloseAsync(userId, alertId, AlertStatus.Resolved, ct);

  /// <summary>The AlertClosed handler plans the "đã an toàn" notices in this same transaction.</summary>
  private async Task<Result> CloseAsync(Guid userId, Guid alertId, AlertStatus target, CancellationToken ct)
  {
    var alert = await alerts.GetOwnedAsync(alertId, userId, ct);
    if (alert is null) return Errors.AlertNotFound;
    var r = alert.Close(target, AlertResolver.User, clock.GetUtcNow());
    if (r.IsFailure) return r.Error;
    await uow.SaveChangesAsync(ct);
    return Result.Success();
  }
}
