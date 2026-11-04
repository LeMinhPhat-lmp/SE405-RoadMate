namespace RoadMate.Services.Tracking;

public class TelemetryService(IUnitOfWork uow, IAlertService alerts, IGroupService groups, TimeProvider clock) : ITelemetryService
{
  public const int MaxBatch = 500;

  public async Task<Result> AddPointsAsync(Guid userId, Guid tripId, AddPointsRequest req, CancellationToken ct)
  {
    if (req.Points.Count == 0) return Result.Success();
    if (req.Points.Count > MaxBatch) return Error.Validation("BATCH_TOO_LARGE", $"Tối đa {MaxBatch} điểm mỗi lần gửi.");
    if (req.Points.Any(p => !Geo.ValidLatLng(p.Lat, p.Lng))) return Errors.InvalidLocation;

    var trip = await uow.TripRepository.GetOwnedActiveAsync(tripId, userId, true, ct);
    if (trip.IsFailure) return trip.Error;

    var pts = req.Points.Select(p => p.ToEntity(tripId)).ToList();
    await uow.TripPointRepository.AddRangeAsync(pts, ct);

    var last = pts.OrderBy(p => p.RecordedAt).Last();
    trip.Value.UpdateLive(last.Lat, last.Lng, last.Speed, last.Battery, true, clock.UtcNow());
    await uow.SaveChangesAsync(ct);
    await alerts.CloseOpenAsync(tripId, AlertType.LostSignal, AlertResolver.System, ct);
    if (trip.Value.GroupId is { } groupId) await groups.ReportLocationAsync(userId, groupId, last.Lat, last.Lng, last.Battery, null, ct);
    return Result.Success();
  }

  public async Task<Result<HeartbeatResponse>> HeartbeatAsync(Guid userId, Guid tripId, HeartbeatRequest req, CancellationToken ct)
  {
    if (!Geo.ValidLatLng(req.Lat, req.Lng)) return Errors.InvalidLocation;
    var trip = await uow.TripRepository.GetOwnedActiveAsync(tripId, userId, true, ct);
    if (trip.IsFailure) return trip.Error;
    trip.Value.UpdateLive(req.Lat, req.Lng, req.Speed, req.Battery, req.NetworkOk, clock.UtcNow());
    await uow.SaveChangesAsync(ct);
    await alerts.CloseOpenAsync(tripId, AlertType.LostSignal, AlertResolver.System, ct);   // tín hiệu trở lại -> tự đóng cảnh báo mất sóng

    var group = trip.Value.GroupId is { } groupId
      ? await groups.ReportLocationAsync(userId, groupId, req.Lat, req.Lng, req.Battery, req.MessagesSince, ct)
      : null;
    return new HeartbeatResponse(group);
  }
}
