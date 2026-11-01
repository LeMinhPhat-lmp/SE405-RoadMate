namespace RoadMate.Entities.Alerts;

public static class AlertType
{
  public const string Crash = "crash", Idle = "idle", Sos = "sos", OffRoute = "offroute",
    LowBattery = "lowBattery", LostSignal = "lostSignal", LateReturn = "lateReturn";
  public static readonly string[] All = { Crash, Idle, Sos, OffRoute, LowBattery, LostSignal, LateReturn };
}
