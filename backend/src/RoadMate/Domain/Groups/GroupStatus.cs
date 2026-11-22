using RoadMate.Domain.Common;

namespace RoadMate.Domain.Groups;

public enum GroupStatus { [WireValue("open")] Open, [WireValue("riding")] Riding, [WireValue("closed")] Closed }
