namespace RoadMate.Common;

public static class TimeProviderExtensions
{
  /// <summary>Giờ hiện tại dạng <see cref="DateTime"/> UTC (Kind = Utc). Dùng thay DateTime.UtcNow để test được.</summary>
  public static DateTime UtcNow(this TimeProvider clock) => clock.GetUtcNow().UtcDateTime;
}
