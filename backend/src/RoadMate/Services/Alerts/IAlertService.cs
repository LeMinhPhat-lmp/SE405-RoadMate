namespace RoadMate.Services.Alerts;

public interface IAlertService
{
  /// <summary>Cảnh báo do app gửi lên (crash, sos, idle…). Idempotent theo <c>ClientId</c>.</summary>
  Task<Result<AlertDto>> CreateAsync(Guid userId, Guid tripId, CreateAlertRequest req, CancellationToken ct);
  /// <summary>Cảnh báo giả lập (Debug) với chế độ gửi <see cref="SimulationMode"/>.</summary>
  Task<Result<AlertDto>> CreateSimulatedAsync(Guid userId, Guid tripId, CreateAlertRequest req, string simulationMode, CancellationToken ct);
  /// <summary>Cảnh báo do server tự sinh (job giám sát): lostSignal, lateReturn.</summary>
  Task<Result<AlertDto>> RaiseSystemAsync(Guid userId, Guid tripId, string type, double lat, double lng, int? battery, CancellationToken ct);
  Task<Result<List<AlertDto>>> ListAsync(Guid userId, Guid tripId, CancellationToken ct);
  Task<Result<AlertDto>> UpdateLocationAsync(Guid userId, Guid alertId, AlertLocationRequest req, CancellationToken ct);
  /// <summary>Đóng cảnh báo bởi chính người dùng. <paramref name="targetStatus"/>: AlertStatus.Cancelled ("Tôi ổn") hoặc AlertStatus.Resolved.</summary>
  Task<Result<AlertDto>> CloseAsync(Guid userId, Guid alertId, string targetStatus, CancellationToken ct);
  /// <summary>
  /// Đóng (Resolved) mọi cảnh báo đang mở của chuyến theo loại (null = mọi loại) và báo người thân nếu họ đã nhận cảnh báo.
  /// Dùng khi: heartbeat trở lại (lostSignal), đổi giờ về (lateReturn), kết thúc chuyến.
  /// </summary>
  Task CloseOpenAsync(Guid tripId, string? type, string resolvedBy, CancellationToken ct);
}
