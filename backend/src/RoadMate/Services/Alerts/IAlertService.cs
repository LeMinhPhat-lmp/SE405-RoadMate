namespace RoadMate.Services.Alerts;

public interface IAlertService
{
  Task<Result<AlertDto>> CreateAsync(Guid userId, Guid tripId, CreateAlertRequest req, CancellationToken ct);
  Task<Result<List<AlertDto>>> ListAsync(Guid userId, Guid tripId, CancellationToken ct);
  Task<Result<AlertDto>> UpdateLocationAsync(Guid userId, Guid alertId, AlertLocationRequest req, CancellationToken ct);
  /// <summary>Đóng cảnh báo bởi chính người dùng. <paramref name="targetStatus"/>: AlertStatus.Cancelled ("Tôi ổn") hoặc AlertStatus.Resolved.</summary>
  Task<Result<AlertDto>> CloseAsync(Guid userId, Guid alertId, string targetStatus, CancellationToken ct);
}
