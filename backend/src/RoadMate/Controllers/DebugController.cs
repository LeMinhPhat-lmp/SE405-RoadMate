namespace RoadMate.Controllers;

/// <summary>Chỉ hoạt động khi Debug:EnableSimulator = true (dev/demo); tắt thì trả 404.</summary>
public class DebugController(IAlertSimulationService simulator) : ApiControllerBase
{
  /// <remarks>
  /// ⚠ CHỈ DÙNG DEV/DEMO. Mặc định dryRun=true: không gửi SMS/push thật. dryRun=false: chỉ gửi thật tới số trong Debug:AllowedPhones.
  /// Cảnh báo giả lập được đánh dấu simulationMode nên các lần lặp/đóng sau đó cũng theo cùng chế độ.
  /// </remarks>
  [HttpPost("debug/simulate-alert")]
  public async Task<IActionResult> Simulate(SimulateAlertRequest req, CancellationToken ct) =>
    HandleCreated(await simulator.SimulateAsync(UserId, req, ct));
}
