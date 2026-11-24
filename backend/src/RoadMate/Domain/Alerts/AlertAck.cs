using RoadMate.Domain.Abstractions;
using RoadMate.Domain.Common;
using RoadMate.Domain.Users;

namespace RoadMate.Domain.Alerts;

/// <summary>Private token per (alert, contact). Acknowledging only stops repeats for that contact; the alert stays open.</summary>
public sealed class AlertAck : Entity
{
  private AlertAck() { }

  public Guid AlertId { get; private set; }
  public Guid ContactId { get; private set; }
  public string Token { get; private set; } = default!;
  public DateTimeOffset ExpiresAt { get; private set; }
  public DateTimeOffset? AckedAt { get; private set; }
  public Alert? Alert { get; private set; }
  public EmergencyContact? Contact { get; private set; }
  public bool IsAcked => AckedAt is not null;

  public static AlertAck Create(Guid alertId, Guid contactId, string token, DateTimeOffset expiresAt) =>
    new() { Id = Guid.NewGuid(), AlertId = alertId, ContactId = contactId, Token = token, ExpiresAt = expiresAt };

  public Result Acknowledge(DateTimeOffset now, bool alertIsOpen)
  {
    if (IsAcked) return Result.Success();                               // idempotent
    if (now >= ExpiresAt || !alertIsOpen) return Errors.AckInvalid;
    AckedAt = now;
    return Result.Success();
  }
}
