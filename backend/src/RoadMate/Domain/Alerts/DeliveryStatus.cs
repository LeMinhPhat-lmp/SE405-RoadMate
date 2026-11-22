using RoadMate.Domain.Common;

namespace RoadMate.Domain.Alerts;

public enum DeliveryStatus { [WireValue("queued")] Queued, [WireValue("processing")] Processing, [WireValue("sent")] Sent, [WireValue("failed")] Failed, [WireValue("skipped")] Skipped }
