namespace RoadMate.Domain.Alerts;

public sealed record DeliveryRetryPolicy(int MaxAttempts, IReadOnlyList<TimeSpan> Backoff)
{
  /// <summary>4 attempts in total; retry after 15 s, 30 s, 60 s.</summary>
  public static readonly DeliveryRetryPolicy Default = new(4, [TimeSpan.FromSeconds(15), TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(60)]);
}
