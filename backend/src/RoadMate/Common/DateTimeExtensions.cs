namespace RoadMate.Common;

public static class DateTimeExtensions
{
  /// <summary>Npgsql (timestamptz) chỉ nhận UTC.</summary>
  public static DateTime ToUtc(this DateTime d) =>
    d.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(d, DateTimeKind.Utc) : d.ToUniversalTime();
  public static DateTime? ToUtc(this DateTime? d) => d?.ToUtc();
}
