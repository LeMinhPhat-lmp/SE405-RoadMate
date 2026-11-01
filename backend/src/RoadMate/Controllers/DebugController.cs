using RoadMate.Infrastructure.Diagnostics;

namespace RoadMate.Controllers;

/// <summary>Chỉ bật khi Debug:EnableSimulator = true (dev).</summary>
public class DebugController(IAlertService svc, IOptions<DebugOptions> options) : ApiControllerBase
{
  /// <remarks>⚠ CHỈ DÙNG DEV/DEMO: cần Debug:EnableSimulator=true, tắt ở production. Gửi SMS/push thật nếu đã cấu hình.</remarks>
  [HttpPost("debug/simulate-alert")]
  public async Task<IActionResult> Simulate(SimulateAlertRequest req, CancellationToken ct)
  {
    if (!options.Value.EnableSimulator) return NotFound();
    var type = string.IsNullOrWhiteSpace(req.Type) ? AlertType.Crash : req.Type;
    var r = await svc.CreateAsync(UserId, req.TripId,
      new CreateAlertRequest("sim-" + Guid.NewGuid().ToString("N"), type, req.Lat ?? 10.7769, req.Lng ?? 106.7009, 80), ct);
    return HandleCreated(r);
  }
}
