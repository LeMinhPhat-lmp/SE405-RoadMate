namespace RoadMate.Services.Alerts;

public class AlertOptions
{
  /// <summary>Chu kỳ lặp cảnh báo nghiêm trọng (crash/sos/idle).</summary>
  public int RepeatMinutes { get; set; } = 2;
  /// <summary>Chu kỳ lặp cảnh báo do server tự sinh (lostSignal/lateReturn) – thưa hơn để không spam người thân.</summary>
  public int SlowRepeatMinutes { get; set; } = 10;
  public int MaxRepeats { get; set; } = 30;
  public int WorkerPollSeconds { get; set; } = 15;
}
