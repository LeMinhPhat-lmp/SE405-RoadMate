using RoadMate.Domain.Common;

namespace RoadMate.Domain.Alerts;

public enum AlertResolver
{
  [WireValue("user")] User,
  [WireValue("contact")] Contact,
  [WireValue("system")] System
}
