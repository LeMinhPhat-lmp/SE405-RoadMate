namespace RoadMate.Common;

public static class CoordinateExtensions
{
  /// <summary>Toạ độ dạng chuỗi 6 chữ số thập phân, invariant culture (dùng cho URL/query/tin nhắn).</summary>
  public static string ToCoordinate(this double d) => d.ToString("F6", CultureInfo.InvariantCulture);
}
