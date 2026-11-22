using RoadMate.Domain.Common;

namespace RoadMate.Domain.Alerts;

public enum NotificationKind
{
  [WireValue("alert")] Initial,
  [WireValue("repeat")] Repeat,
  [WireValue("closed")] Closed
}
