namespace RoadMate.Controllers;

public class TelemetryController(ITelemetryService svc) : ApiControllerBase
{
  [HttpPost("trips/{id:guid}/points")]
  public async Task<IActionResult> AddPoints(Guid id, AddPointsRequest req, CancellationToken ct) => Handle(await svc.AddPointsAsync(UserId, id, req, ct));

  /// <remarks>
  /// Lưu vị trí/pin/mạng; server dùng để phát hiện mất tín hiệu và về trễ. Nếu chuyến đã gắn nhóm, response chứa vị trí các
  /// thành viên và tin nhắn mới (gửi <c>messagesSince</c> để nhận). Pin yếu/lệch tuyến/dừng lâu do app tự gửi POST alerts.
  /// </remarks>
  [HttpPost("trips/{id:guid}/heartbeat")]
  public async Task<IActionResult> Heartbeat(Guid id, HeartbeatRequest req, CancellationToken ct) => Handle(await svc.HeartbeatAsync(UserId, id, req, ct));
}
