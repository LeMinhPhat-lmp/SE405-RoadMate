namespace RoadMate.Services.Alerts;

/// <summary>Giả lập cảnh báo cho demo/bảo vệ đồ án (cần Debug:EnableSimulator=true).</summary>
public interface IAlertSimulationService
{
  Task<Result<AlertDto>> SimulateAsync(Guid userId, SimulateAlertRequest req, CancellationToken ct);
}
