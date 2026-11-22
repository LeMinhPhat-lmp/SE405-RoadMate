using RoadMate.Domain.Abstractions;
using RoadMate.Domain.Common;
using RoadMate.Domain.Events;
using RoadMate.Domain.Trips;
using RoadMate.Domain.Users;
using RoadMate.Domain.ValueObjects;

namespace RoadMate.Domain.Alerts;

/// <summary>Auditable but NOT soft-deletable: an alert is evidence and must never be hidden or removed.</summary>
public sealed class Alert : AuditableEntity, IAggregateRoot
{
  private readonly List<NotificationDelivery> _deliveries = [];
  private readonly List<AlertAck> _acks = [];

  private Alert() { }

  public Guid TripId { get; private set; }
  public Guid UserId { get; private set; }
  public string ClientId { get; private set; } = default!;
  public AlertType Type { get; private set; }
  public AlertStatus Status { get; private set; }
  public double Lat { get; private set; }
  public double Lng { get; private set; }
  public int? Battery { get; private set; }
  public int RepeatCount { get; private set; }
  public DateTimeOffset? NextRepeatAt { get; private set; }
  public DateTimeOffset? ResolvedAt { get; private set; }
  public AlertResolver? ResolvedBy { get; private set; }
  public SimulationMode? Simulation { get; private set; }
  /// <summary>Decided at raise time from the type policy; used to decide whether closing must notify contacts.</summary>
  public bool NotifiesContacts { get; private set; }
  public string? LockedBy { get; private set; }
  public DateTimeOffset? LockedUntil { get; private set; }

  public Trip? Trip { get; private set; }
  public User? User { get; private set; }
  public IReadOnlyCollection<NotificationDelivery> Deliveries => _deliveries;
  public IReadOnlyCollection<AlertAck> Acks => _acks;

  public GeoPoint Location => GeoPoint.FromTrusted(Lat, Lng);
  public bool IsOpen => Status is AlertStatus.Pending or AlertStatus.Sent;

  /// <summary>New alert: Pending, RepeatCount 0, first repeat at now+interval when the policy notifies contacts and repeats.</summary>
  public static Alert Raise(Guid tripId, Guid userId, ClientRequestId clientId, AlertType type, GeoPoint at, int? battery,
    SimulationMode? simulation, IAlertTypePolicy policy, TimeSpan? repeatInterval, DateTimeOffset now)
  {
    var alert = new Alert
    {
      Id = Guid.NewGuid(),
      TripId = tripId,
      UserId = userId,
      ClientId = clientId.Value,
      Type = type,
      Status = AlertStatus.Pending,
      Lat = at.Lat,
      Lng = at.Lng,
      Battery = battery,
      Simulation = simulation,
      NotifiesContacts = policy.NotifiesContacts,
      NextRepeatAt = policy.NotifiesContacts && repeatInterval is { } i ? now + i : null
    };
    alert.AddEvent(new AlertRaised(alert.Id, tripId, now));
    return alert;
  }

  public Result UpdateLocation(GeoPoint at, int? battery)
  {
    if (!IsOpen) return Errors.AlertClosed;
    Lat = at.Lat; Lng = at.Lng; Battery = battery ?? Battery;
    return Result.Success();
  }

  public Result Close(AlertStatus target, AlertResolver by, DateTimeOffset now)
  {
    if (target is not (AlertStatus.Cancelled or AlertStatus.Resolved)) return Errors.InvalidStatus;
    if (Status == target) return Result.Success();                         // idempotent: no event, ResolvedAt unchanged
    if (!IsOpen) return Error.Conflict(Errors.AlertClosed.Code, $"Cảnh báo đã ở trạng thái {EnumWire.ToWire(Status)}.");

    var wasNotified = RepeatCount > 0 && NotifiesContacts;
    Status = target; ResolvedAt = now; ResolvedBy = by; NextRepeatAt = null; LockedBy = null; LockedUntil = null;
    AddEvent(new AlertClosed(Id, TripId, UserId, target, by, wasNotified, now));
    return Result.Success();
  }

  /// <summary>Pending → Sent (first successful delivery). No-op otherwise.</summary>
  public void MarkDelivered(DateTimeOffset now)
  {
    if (Status == AlertStatus.Pending) Status = AlertStatus.Sent;
  }

  /// <summary>Decides what the repeat scheduler must do and applies the state change (no sending here).</summary>
  public RepeatDecision PlanRepeat(DateTimeOffset now, TimeSpan? interval, int maxRepeats, bool allContactsAcked, bool tripFinished)
  {
    if (!IsOpen) return RepeatDecision.Stop;
    if (tripFinished)
    {
      Close(AlertStatus.Resolved, AlertResolver.System, now);
      return RepeatDecision.Closed;
    }
    if (RepeatCount >= maxRepeats)
    {
      NextRepeatAt = null;
      return RepeatDecision.Stop;
    }
    RepeatCount++;
    NextRepeatAt = allContactsAcked || interval is null ? null : now + interval;
    return RepeatDecision.Send;
  }

  /// <summary>Counts the initial send: RepeatCount = 1 and (when nobody can be notified) no more repeats.</summary>
  public void MarkInitialDispatched(bool hasRecipients, bool allAcked)
  {
    RepeatCount = 1;
    if (!hasRecipients || allAcked) NextRepeatAt = null;
  }

  /// <summary>Postpone the next repeat (a failing alert must not be retried in a hot loop).</summary>
  public void PostponeRepeat(DateTimeOffset until) { if (IsOpen) NextRepeatAt = until; }
}
