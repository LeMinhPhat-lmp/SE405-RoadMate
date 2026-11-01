namespace RoadMate.Controllers;

public class TelemetryController(ITelemetryService svc) : ApiControllerBase
{
  [HttpPost("trips/{id:guid}/points")]
  public async Task<IActionResult> AddPoints(Guid id, AddPointsRequest req, CancellationToken ct) => Handle(await svc.AddPointsAsync(UserId, id, req, ct));

  /// <remarks>
  /// ⚠ CHỈ LƯU vị trí/pin/mạng. Chưa có job nền dùng dữ liệu này (mất sóng, pin yếu, về trễ, tụt đoàn) nên các cảnh báo đó
  /// hiện chỉ phát sinh khi app tự gọi POST alerts.
  /// </remarks>
  [HttpPost("trips/{id:guid}/heartbeat")]
  public async Task<IActionResult> Heartbeat(Guid id, HeartbeatRequest req, CancellationToken ct) => Handle(await svc.HeartbeatAsync(UserId, id, req, ct));
}
