namespace RoadMate.Entities.Groups;

public static class GroupStatus
{
  /// <summary>Đang tập hợp, nhận thành viên.</summary>
  public const string Open = "open";
  /// <summary>Đã xuất phát: bật cảnh báo tụt đoàn.</summary>
  public const string Riding = "riding";
  /// <summary>Đã kết thúc: không nhận thành viên, không nhắn tin.</summary>
  public const string Closed = "closed";
}
