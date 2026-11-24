namespace RoadMate.Application.Common;

/// <summary>Wire DTOs keep <see cref="DateTime"/> (UTC, serialized with "Z") for contract compatibility; the domain uses <see cref="DateTimeOffset"/>.</summary>
public static class TimeExtensions
{
  /// <summary>Incoming DateTime (any Kind) → UTC DateTimeOffset. Unspecified is treated as UTC (Npgsql timestamptz requirement).</summary>
  public static DateTimeOffset ToUtcOffset(this DateTime d) =>
    new(d.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(d, DateTimeKind.Utc) : d.ToUniversalTime(), TimeSpan.Zero);

  public static DateTimeOffset? ToUtcOffset(this DateTime? d) => d?.ToUtcOffset();

  /// <summary>DateTimeOffset → UTC DateTime (Kind = Utc) for response DTOs.</summary>
  public static DateTime ToWire(this DateTimeOffset d) => d.UtcDateTime;
  public static DateTime? ToWire(this DateTimeOffset? d) => d?.UtcDateTime;
}
