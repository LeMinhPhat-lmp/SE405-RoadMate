using RoadMate.Domain.Common;

namespace RoadMate.Domain.Groups;

public enum GroupRole { [WireValue("leader")] Leader, [WireValue("member")] Member }
