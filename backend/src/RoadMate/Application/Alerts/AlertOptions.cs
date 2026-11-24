using RoadMate.Domain.Alerts;

namespace RoadMate.Application.Alerts;

public class AlertOptions
{
  /// <summary>Repeat interval of serious alerts (crash/sos/idle).</summary>
  public int RepeatMinutes { get; set; } = 2;
  /// <summary>Repeat interval of server-raised alerts (lostSignal/lateReturn): sparser so relatives are not spammed.</summary>
  public int SlowRepeatMinutes { get; set; } = 10;
  public int MaxRepeats { get; set; } = 30;
  public int WorkerPollSeconds { get; set; } = 15;
  /// <summary>How long a per-contact acknowledgement link stays valid.</summary>
  public int AckLinkHours { get; set; } = 72;
  /// <summary>Outbox: lease on a claimed delivery, batch size and per-row attempts.</summary>
  public int DeliveryLeaseSeconds { get; set; } = 60;
  public int DeliveryBatchSize { get; set; } = 20;
  public int DeliveryMaxAttempts { get; set; } = 4;

  public DeliveryRetryPolicy Retry => new(DeliveryMaxAttempts, DeliveryRetryPolicy.Default.Backoff);
  public AlertTiming Timing => new(TimeSpan.FromMinutes(RepeatMinutes), TimeSpan.FromMinutes(SlowRepeatMinutes));
}
