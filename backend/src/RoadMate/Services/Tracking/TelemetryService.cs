namespace RoadMate.Services.Tracking;

public class TelemetryService(IUnitOfWork uow) : ITelemetryService
{
  public const int MaxBatch = 500;

  public async Task<Result> AddPointsAsync(Guid userId, Guid tripId, AddPointsRequest req, CancellationToken ct)
  {
    if (req.Points.Count == 0) return Result.Success();
    if (req.Points.Count > MaxBatch) return Error.Validation("BATCH_TOO_LARGE", $"Tối đa {MaxBatch} điểm mỗi lần gửi.");
    if (req.Points.Any(p => !Geo.ValidLatLng(p.Lat, p.Lng))) return Error.Validation("INVALID_LOCATION", "Có toạ độ không hợp lệ.");

    var trip = await uow.TripRepository.GetOwnedActiveAsync(tripId, userId, true, ct);
    if (trip.IsFailure) return trip.Error;

    var pts = req.Points.Select(p => p.ToEntity(tripId)).ToList();
    await uow.TripPointRepository.AddRangeAsync(pts, ct);

    var last = pts.OrderBy(p => p.RecordedAt).Last();
    trip.Value.UpdateLive(last.Lat, last.Lng, last.Speed, last.Battery, true, DateTime.UtcNow);
    await uow.SaveChangesAsync(ct);
    return Result.Success();
  }

  public async Task<Result> HeartbeatAsync(Guid userId, Guid tripId, HeartbeatRequest req, CancellationToken ct)
  {
    if (!Geo.ValidLatLng(req.Lat, req.Lng)) return Errors.InvalidLocation;
    var trip = await uow.TripRepository.GetOwnedActiveAsync(tripId, userId, true, ct);
    if (trip.IsFailure) return trip.Error;
    trip.Value.UpdateLive(req.Lat, req.Lng, req.Speed, req.Battery, req.NetworkOk, DateTime.UtcNow);
    await uow.SaveChangesAsync(ct);
    return Result.Success();
  }
}
