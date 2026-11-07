namespace RoadMate.Entities.Alerts;

/// <summary>Chế độ của cảnh báo giả lập (Alert.SimulationMode). null = cảnh báo thật.</summary>
public static class SimulationMode
{
  /// <summary>Chạy đủ luồng nhưng KHÔNG gửi SMS/push thật (chỉ ghi log + delivery "dry-run").</summary>
  public const string DryRun = "dryRun";
  /// <summary>Chỉ gửi thật tới số nằm trong Debug:AllowedPhones; số khác vẫn dry-run.</summary>
  public const string Live = "live";
}
