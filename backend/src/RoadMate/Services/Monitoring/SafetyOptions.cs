namespace RoadMate.Services.Monitoring;

/// <summary>Ngưỡng cho các cảnh báo do SERVER tự phát hiện (section "Safety").</summary>
public class SafetyOptions
{
  /// <summary>Không nhận heartbeat/điểm GPS quá số phút này -> cảnh báo lostSignal.</summary>
  public int LostSignalMinutes { get; set; } = 10;
  /// <summary>Quá giờ dự kiến về + số phút này -> nhắc chủ chuyến.</summary>
  public int LateReturnGraceMinutes { get; set; } = 60;
  /// <summary>Sau khi nhắc chủ chuyến, chờ số phút này mà chưa phản hồi -> SMS người thân.</summary>
  public int LateReturnOwnerResponseMinutes { get; set; } = 15;
  public int MonitorIntervalSeconds { get; set; } = 30;
}
