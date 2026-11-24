using RoadMate.Domain.Common;

namespace RoadMate.Domain.Alerts;

public enum AlertType
{
  [WireValue("crash")] Crash,
  [WireValue("idle")] Idle,
  [WireValue("sos")] Sos,
  [WireValue("offroute")] OffRoute,
  [WireValue("lowBattery")] LowBattery,
  [WireValue("lostSignal")] LostSignal,
  [WireValue("lateReturn")] LateReturn,
}
