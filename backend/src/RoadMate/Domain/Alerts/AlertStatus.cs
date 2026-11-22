using RoadMate.Domain.Common;

namespace RoadMate.Domain.Alerts;

public enum AlertStatus { [WireValue("pending")] Pending, [WireValue("sent")] Sent, [WireValue("resolved")] Resolved, [WireValue("cancelled")] Cancelled }
