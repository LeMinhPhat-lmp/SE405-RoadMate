namespace RoadMate.Data;

public static class SqlFilters
{
  /// <summary>Điều kiện lọc cho unique index chỉ áp dụng với bản ghi chưa xoá mềm.</summary>
  public const string NotDeleted = "\"DeletedAt\" IS NULL";
}
