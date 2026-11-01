namespace RoadMate.Services.Tracking;

/// <summary>Nhận dữ liệu từ thiết bị trong chuyến: điểm GPS theo lô và heartbeat (vị trí/pin/mạng).</summary>
public interface ITelemetryService
{
  Task<Result> AddPointsAsync(Guid userId, Guid tripId, AddPointsRequest req, CancellationToken ct);
  Task<Result> HeartbeatAsync(Guid userId, Guid tripId, HeartbeatRequest req, CancellationToken ct);
}
