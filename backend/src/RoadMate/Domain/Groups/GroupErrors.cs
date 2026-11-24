using RoadMate.Domain.Common;

namespace RoadMate.Domain.Groups;

public static class GroupErrors
{
  /// <summary>Also used when the caller is not a member, so the group's existence is not leaked.</summary>
  public static readonly Error NotFound = Error.NotFound("GROUP_NOT_FOUND", "Không tìm thấy nhóm.");
  public static readonly Error CodeNotFound = Error.NotFound("GROUP_CODE_NOT_FOUND", "Mã nhóm không tồn tại.");
  public static readonly Error InvalidCode = Error.Validation("INVALID_GROUP_CODE", "Mã nhóm gồm 6 ký tự chữ/số.");
  public static readonly Error NotLeader = Error.Forbidden("NOT_GROUP_LEADER", "Chỉ trưởng đoàn mới thực hiện được thao tác này.");
  public static readonly Error Closed = Error.Conflict("GROUP_CLOSED", "Nhóm đã kết thúc.");
  public static readonly Error MemberNotFound = Error.NotFound("MEMBER_NOT_FOUND", "Người này không ở trong nhóm.");
  public static readonly Error CannotRemoveSelf = Error.Validation("CANNOT_REMOVE_SELF", "Muốn rời nhóm hãy dùng chức năng rời nhóm.");
  public static readonly Error InvalidDepartTime = Error.Validation("INVALID_DEPART_TIME", "Giờ xuất phát phải ở tương lai.");
  public static Error Full(int max) => Error.Conflict("GROUP_FULL", $"Nhóm đã đủ {max} thành viên.");
}
