using RoadMate.Domain.Common;

namespace RoadMate.Domain.Alerts;

public enum DeliveryChannel
{
  [WireValue("sms")] Sms,
  [WireValue("push")] Push,
  [WireValue("email")] Email
}
