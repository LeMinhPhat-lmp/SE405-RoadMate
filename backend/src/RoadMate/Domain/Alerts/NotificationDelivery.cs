using RoadMate.Domain.Abstractions;
using RoadMate.Domain.Common;
using RoadMate.Domain.Users;

namespace RoadMate.Domain.Alerts;

/// <summary>
/// Transactional-outbox row: one SMS/push to one contact for one alert event. Unique (AlertId, ContactId, Channel, Kind, Sequence),
/// so planning is idempotent. State machine: Queued → Processing → Sent | Queued(retry) | Failed; Skipped is terminal (dry-run).
/// </summary>
public sealed class NotificationDelivery : Entity
{
  private NotificationDelivery() { }

  public Guid AlertId { get; private set; }
  public Guid? ContactId { get; private set; }
  public DeliveryChannel Channel { get; private set; }
  public NotificationKind Kind { get; private set; }
  public int Sequence { get; private set; }
  public DeliveryStatus Status { get; private set; }
  public int Attempts { get; private set; }
  public DateTimeOffset NextAttemptAt { get; private set; }
  public DateTimeOffset? LockedUntil { get; private set; }
  public string? LockedBy { get; private set; }
  public string? ProviderMessageId { get; private set; }
  public string? Error { get; private set; }
  public DateTimeOffset CreatedAt { get; private set; }
  public DateTimeOffset? SentAt { get; private set; }
  public Alert? Alert { get; private set; }
  public EmergencyContact? Contact { get; private set; }

  public static NotificationDelivery Queue(Guid alertId, Guid? contactId, DeliveryChannel channel, NotificationKind kind, int sequence, DateTimeOffset now) =>
    new()
    {
      Id = Guid.NewGuid(),
      AlertId = alertId,
      ContactId = contactId,
      Channel = channel,
      Kind = kind,
      Sequence = sequence,
      Status = DeliveryStatus.Queued,
      NextAttemptAt = now,
      CreatedAt = now
    };

  public Result Skip(string reason, DateTimeOffset now)
  {
    if (Status != DeliveryStatus.Queued)
    {
      return Errors.DeliveryInvalidState;
    }
    Status = DeliveryStatus.Skipped;
    ProviderMessageId = reason;
    SentAt = null;
    NextAttemptAt = now;
    return Result.Success();
  }

  public Result Abandon(string reason, DateTimeOffset now)
  {
    if (Status != DeliveryStatus.Queued && Status != DeliveryStatus.Processing)
    {
      return Errors.DeliveryInvalidState;
    }
    Status = DeliveryStatus.Skipped;
    ProviderMessageId = reason;
    LockedBy = null;
    LockedUntil = null;
    NextAttemptAt = now;
    return Result.Success();
  }

  public Result Claim(string workerId, DateTimeOffset now, TimeSpan lease)
  {
    bool eligible = (Status == DeliveryStatus.Queued && NextAttemptAt <= now)
                 || (Status == DeliveryStatus.Processing && LockedUntil.HasValue && LockedUntil.Value < now);
    if (!eligible)
    {
      return Errors.DeliveryInvalidState;
    }
    Status = DeliveryStatus.Processing;
    Attempts++;
    LockedBy = workerId;
    LockedUntil = now + lease;
    return Result.Success();
  }

  public Result MarkSent(string providerMessageId, DateTimeOffset now)
  {
    if (Status != DeliveryStatus.Processing)
    {
      return Errors.DeliveryInvalidState;
    }
    Status = DeliveryStatus.Sent;
    ProviderMessageId = providerMessageId;
    SentAt = now;
    Error = null;
    LockedBy = null;
    LockedUntil = null;
    return Result.Success();
  }

  public Result MarkFailed(string error, DateTimeOffset now, DeliveryRetryPolicy policy)
  {
    if (Status != DeliveryStatus.Processing)
    {
      return Errors.DeliveryInvalidState;
    }
    Error = error.Length > 500 ? error.Substring(0, 500) : error;
    LockedBy = null;
    LockedUntil = null;
    if (Attempts < policy.MaxAttempts)
    {
      Status = DeliveryStatus.Queued;
      var backoffIndex = Math.Min(Attempts, policy.Backoff.Count) - 1;
      NextAttemptAt = now + policy.Backoff[backoffIndex];
    }
    else
    {
      Status = DeliveryStatus.Failed;
    }
    return Result.Success();
  }
}
