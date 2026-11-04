namespace RoadMate.Services.Alerts;

/// <summary>Chính sách theo loại cảnh báo: có báo người thân không, lặp bao lâu một lần (hàm thuần, dễ test).</summary>
public static class AlertPolicy
{
  /// <summary>offroute chỉ để app nhắc chủ chuyến, không SMS người thân.</summary>
  public static bool NotifiesContacts(string type) => type != AlertType.OffRoute;

  /// <summary>null = không lặp (chỉ gửi một lần): lowBattery, offroute.</summary>
  public static TimeSpan? RepeatInterval(string type, AlertOptions o) => type switch
  {
    AlertType.Crash or AlertType.Sos or AlertType.Idle => TimeSpan.FromMinutes(o.RepeatMinutes),
    AlertType.LostSignal or AlertType.LateReturn => TimeSpan.FromMinutes(o.SlowRepeatMinutes),
    _ => null
  };
}
