using RoadMate.Domain.Common;

namespace RoadMate.Domain.Users;

public enum CrashSensitivity { [WireValue("low")] Low, [WireValue("medium")] Medium, [WireValue("high")] High }
