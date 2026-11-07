namespace RoadMate.Services.Groups;

public static class GroupErrors
{
  /// <summary>Dùng cả khi người gọi không phải thành viên, để không lộ sự tồn tại của nhóm.</summary>
  public static readonly Error NotFound = Error.NotFound("GROUP_NOT_FOUND", "Không tìm thấy nhóm.");
  public static readonly Error CodeNotFound = Error.NotFound("GROUP_CODE_NOT_FOUND", "Mã nhóm không tồn tại.");
  public static readonly Error InvalidCode = Error.Validation("INVALID_GROUP_CODE", "Mã nhóm gồm 6 ký tự chữ/số.");
  public static readonly Error NotLeader = Error.Forbidden("NOT_GROUP_LEADER", "Chỉ trưởng đoàn mới thực hiện được thao tác này.");
  public static readonly Error Closed = Error.Conflict("GROUP_CLOSED", "Nhóm đã kết thúc.");
  public static readonly Error MemberNotFound = Error.NotFound("MEMBER_NOT_FOUND", "Người này không ở trong nhóm.");
}
