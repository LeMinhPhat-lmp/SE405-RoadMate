namespace RoadMate.Services.Alerts;

/// <summary>Người thân xác nhận "đã liên lạc được" bằng link riêng trong SMS (không cần đăng nhập).</summary>
public interface IAlertAckService
{
  /// <summary>Ghi nhận xác nhận của đúng liên hệ sở hữu token: dừng SMS lặp cho người này, cảnh báo vẫn mở. Idempotent.</summary>
  Task<Result<AckResultDto>> AckAsync(string token, CancellationToken ct);
}
