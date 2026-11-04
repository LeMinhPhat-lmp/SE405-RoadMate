using RoadMate.Infrastructure.Diagnostics;

namespace RoadMate.Services.Alerts;

public class AlertSimulationService(IAlertService alerts, IOptions<DebugOptions> options) : IAlertSimulationService
{
  // Quảng trường Hồ Chí Minh – toạ độ mặc định khi không truyền lat/lng.
  private const double DefaultLat = 10.7769, DefaultLng = 106.7009;
  private const int DefaultBattery = 80;

  public Task<Result<AlertDto>> SimulateAsync(Guid userId, SimulateAlertRequest req, CancellationToken ct)
  {
    if (!options.Value.EnableSimulator)
      return Task.FromResult<Result<AlertDto>>(Error.NotFound("SIMULATOR_DISABLED", "Simulator đang tắt."));

    var type = string.IsNullOrWhiteSpace(req.Type) ? AlertType.Crash : req.Type;
    // Mặc định dry-run: chạy đủ luồng nhưng KHÔNG gửi SMS/push thật. dryRun=false chỉ gửi thật tới số trong Debug:AllowedPhones.
    var mode = req.DryRun == false ? SimulationMode.Live : SimulationMode.DryRun;
    var alert = new CreateAlertRequest("sim-" + Guid.NewGuid().ToString("N"), type, req.Lat ?? DefaultLat, req.Lng ?? DefaultLng, DefaultBattery);
    return alerts.CreateSimulatedAsync(userId, req.TripId, alert, mode, ct);
  }
}
