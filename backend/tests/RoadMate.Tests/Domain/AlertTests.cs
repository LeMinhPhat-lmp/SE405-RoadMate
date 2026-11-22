using System;
using System.Linq;
using RoadMate.Domain.Alerts;
using RoadMate.Domain.Common;
using RoadMate.Domain.Events;
using RoadMate.Tests.TestKit;

namespace RoadMate.Tests.Domain;

public class AlertTests
{
  private static readonly TimeSpan Fast = TimeSpan.FromMinutes(2), Slow = TimeSpan.FromMinutes(10);
  private static DateTimeOffset T0 => Make.Now;

  [Fact, Trait("Scenario", "D-ALERT-01")]
  public void Raise_crash()
  {
    var a = Make.Alert(AlertType.Crash);
    Assert.Equal(AlertStatus.Pending, a.Status); Assert.Equal(0, a.RepeatCount); Assert.Equal(T0 + Fast, a.NextRepeatAt);
    Assert.Single(a.DomainEvents.OfType<AlertRaised>());
  }

  [Theory, Trait("Scenario", "D-ALERT-02"), Trait("Scenario", "D-ALERT-03")]
  [InlineData(AlertType.LowBattery), InlineData(AlertType.OffRoute)]
  public void No_repeat_types(AlertType t) => Assert.Null(Make.Alert(t).NextRepeatAt);

  [Fact, Trait("Scenario", "D-ALERT-03")]
  public void OffRoute_never_notifies() => Assert.False(AlertPolicyResolver.CreateDefault().For(AlertType.OffRoute).NotifiesContacts);

  [Theory, Trait("Scenario", "D-ALERT-04")]
  [InlineData(AlertType.LostSignal), InlineData(AlertType.LateReturn)]
  public void Slow_types(AlertType t) => Assert.Equal(T0 + Slow, Make.Alert(t).NextRepeatAt);

  [Fact, Trait("Scenario", "D-ALERT-05")]
  public void Close_cancel_pending()
  {
    var a = Make.Alert(); var now = T0.AddMinutes(1);
    a.Close(AlertStatus.Cancelled, AlertResolver.User, now).ThrowIfFailed();
    Assert.Equal(AlertStatus.Cancelled, a.Status); Assert.Equal(now, a.ResolvedAt); Assert.Equal(AlertResolver.User, a.ResolvedBy); Assert.Null(a.NextRepeatAt);
    Assert.False(Assert.Single(a.DomainEvents.OfType<AlertClosed>()).WasNotified);
  }

  [Fact, Trait("Scenario", "D-ALERT-06")]
  public void Close_after_notification_was_notified()
  {
    var a = Make.Alert(); a.MarkDelivered(T0); a.MarkInitialDispatched(true, false);
    a.Close(AlertStatus.Resolved, AlertResolver.User, T0.AddMinutes(1));
    Assert.True(Assert.Single(a.DomainEvents.OfType<AlertClosed>()).WasNotified);
  }

  [Fact, Trait("Scenario", "D-ALERT-07")]
  public void Close_is_idempotent()
  {
    var a = Make.Alert(); a.Close(AlertStatus.Resolved, AlertResolver.User, T0.AddMinutes(1));
    var at = a.ResolvedAt;
    Assert.True(a.Close(AlertStatus.Resolved, AlertResolver.User, T0.AddMinutes(9)).IsSuccess);
    Assert.Single(a.DomainEvents.OfType<AlertClosed>()); Assert.Equal(at, a.ResolvedAt);
  }

  [Fact, Trait("Scenario", "D-ALERT-08")]
  public void Close_to_other_state_conflicts()
  {
    var a = Make.Alert(); a.Close(AlertStatus.Cancelled, AlertResolver.User, T0);
    a.Close(AlertStatus.Resolved, AlertResolver.User, T0).ShouldFailWith("ALERT_CLOSED");
    var b = Make.Alert(); b.Close(AlertStatus.Resolved, AlertResolver.User, T0);
    b.Close(AlertStatus.Cancelled, AlertResolver.User, T0).ShouldFailWith("ALERT_CLOSED");
  }

  [Theory, Trait("Scenario", "D-ALERT-09")]
  [InlineData(AlertStatus.Pending), InlineData(AlertStatus.Sent)]
  public void Close_requires_final_status(AlertStatus s) => Make.Alert().Close(s, AlertResolver.User, T0).ShouldFailWith("INVALID_STATUS");

  [Fact, Trait("Scenario", "D-ALERT-10")]
  public void OffRoute_close_is_not_notified()
  {
    var a = Make.Alert(AlertType.OffRoute); a.MarkInitialDispatched(true, false);
    a.Close(AlertStatus.Resolved, AlertResolver.User, T0);
    Assert.False(Assert.Single(a.DomainEvents.OfType<AlertClosed>()).WasNotified);
  }

  [Fact, Trait("Scenario", "D-ALERT-11")]
  public void UpdateLocation_keeps_battery_when_null()
  {
    var a = Make.Alert();
    a.UpdateLocation(Make.Point(1, 2), null).ThrowIfFailed();
    Assert.Equal(1, a.Lat); Assert.Equal(80, a.Battery);
    a.UpdateLocation(Make.Point(3, 4), 50);
    Assert.Equal(50, a.Battery);
  }

  [Fact, Trait("Scenario", "D-ALERT-12")]
  public void UpdateLocation_when_closed()
  {
    var a = Make.Alert(); a.Close(AlertStatus.Resolved, AlertResolver.User, T0);
    var lat = a.Lat;
    a.UpdateLocation(Make.Point(1, 2), 5).ShouldFailWith("ALERT_CLOSED");
    Assert.Equal(lat, a.Lat);
  }

  [Fact, Trait("Scenario", "D-ALERT-13")]
  public void MarkDelivered()
  {
    var p = Make.Alert(); p.MarkDelivered(T0); Assert.Equal(AlertStatus.Sent, p.Status);
    p.MarkDelivered(T0); Assert.Equal(AlertStatus.Sent, p.Status);
    var c = Make.Alert(); c.Close(AlertStatus.Cancelled, AlertResolver.User, T0); c.MarkDelivered(T0);
    Assert.Equal(AlertStatus.Cancelled, c.Status);
  }

  [Fact, Trait("Scenario", "D-ALERT-14")]
  public void PlanRepeat_trip_finished_closes()
  {
    var a = Make.Alert();
    Assert.Equal(RepeatDecision.Closed, a.PlanRepeat(T0, Fast, 10, false, tripFinished: true));
    Assert.Equal(AlertStatus.Resolved, a.Status); Assert.Equal(AlertResolver.System, a.ResolvedBy); Assert.Equal(0, a.RepeatCount);
  }

  [Fact, Trait("Scenario", "D-ALERT-15")]
  public void PlanRepeat_max_reached_stops_but_stays_open()
  {
    var a = Make.Alert(); a.MarkInitialDispatched(true, false);
    Assert.Equal(RepeatDecision.Stop, a.PlanRepeat(T0, Fast, 1, false, false));
    Assert.Null(a.NextRepeatAt); Assert.True(a.IsOpen);
  }

  [Fact, Trait("Scenario", "D-ALERT-16")]
  public void PlanRepeat_normal()
  {
    var a = Make.Alert(); a.MarkInitialDispatched(true, false); var now = T0.AddMinutes(2);
    Assert.Equal(RepeatDecision.Send, a.PlanRepeat(now, Fast, 10, false, false));
    Assert.Equal(2, a.RepeatCount); Assert.Equal(now + Fast, a.NextRepeatAt);
  }

  [Fact, Trait("Scenario", "D-ALERT-17")]
  public void PlanRepeat_all_acked_stops_scheduling()
  {
    var a = Make.Alert(); a.MarkInitialDispatched(true, false);
    Assert.Equal(RepeatDecision.Send, a.PlanRepeat(T0, Fast, 10, true, false));
    Assert.Equal(2, a.RepeatCount); Assert.Null(a.NextRepeatAt);
  }

  [Fact, Trait("Scenario", "D-ALERT-18")]
  public void PlanRepeat_null_interval()
  {
    var a = Make.Alert(); a.MarkInitialDispatched(true, false);
    a.PlanRepeat(T0, null, 10, false, false);
    Assert.Null(a.NextRepeatAt);
  }

  [Fact, Trait("Scenario", "D-ALERT-19")]
  public void PlanRepeat_closed_is_noop()
  {
    var a = Make.Alert(); a.Close(AlertStatus.Cancelled, AlertResolver.User, T0); var rc = a.RepeatCount;
    Assert.Equal(RepeatDecision.Stop, a.PlanRepeat(T0, Fast, 10, false, false));
    Assert.Equal(rc, a.RepeatCount);
  }
}

public class AlertAckTests
{
  [Fact, Trait("Scenario", "D-ACK-01")]
  public void Create() { var k = AlertAck.Create(Guid.NewGuid(), Guid.NewGuid(), "tok", Make.Now.AddHours(1)); Assert.Null(k.AckedAt); Assert.Equal(Make.Now.AddHours(1), k.ExpiresAt); }

  [Fact, Trait("Scenario", "D-ACK-02")]
  public void Acknowledge_ok() { var k = AlertAck.Create(Guid.NewGuid(), Guid.NewGuid(), "t", Make.Now.AddHours(1)); k.Acknowledge(Make.Now, true).ThrowIfFailed(); Assert.Equal(Make.Now, k.AckedAt); }

  [Fact, Trait("Scenario", "D-ACK-03")]
  public void Acknowledge_after_expiry() => AlertAck.Create(Guid.NewGuid(), Guid.NewGuid(), "t", Make.Now).Acknowledge(Make.Now.AddMinutes(1), true).ShouldFailWith("ACK_LINK_INVALID");

  [Fact, Trait("Scenario", "D-ACK-04")]
  public void Acknowledge_closed_alert() => AlertAck.Create(Guid.NewGuid(), Guid.NewGuid(), "t", Make.Now.AddHours(1)).Acknowledge(Make.Now, false).ShouldFailWith("ACK_LINK_INVALID");

  [Fact, Trait("Scenario", "D-ACK-05")]
  public void Acknowledge_twice()
  {
    var k = AlertAck.Create(Guid.NewGuid(), Guid.NewGuid(), "t", Make.Now.AddHours(1));
    k.Acknowledge(Make.Now, true); Assert.True(k.Acknowledge(Make.Now.AddMinutes(5), true).IsSuccess);
    Assert.Equal(Make.Now, k.AckedAt);
  }
}

public class AlertPolicyTests
{
  private static readonly AlertTiming Timing = new(TimeSpan.FromMinutes(2), TimeSpan.FromMinutes(10));

  [Fact, Trait("Scenario", "D-POLICY-01")]
  public void Table()
  {
    var r = AlertPolicyResolver.CreateDefault();
    foreach (var t in new[] { AlertType.Crash, AlertType.Sos, AlertType.Idle }) { Assert.True(r.For(t).NotifiesContacts); Assert.Equal(Timing.Fast, r.For(t).RepeatInterval(Timing)); }
    foreach (var t in new[] { AlertType.LostSignal, AlertType.LateReturn }) { Assert.True(r.For(t).NotifiesContacts); Assert.Equal(Timing.Slow, r.For(t).RepeatInterval(Timing)); }
    Assert.True(r.For(AlertType.LowBattery).NotifiesContacts); Assert.Null(r.For(AlertType.LowBattery).RepeatInterval(Timing));
    Assert.False(r.For(AlertType.OffRoute).NotifiesContacts); Assert.Null(r.For(AlertType.OffRoute).RepeatInterval(Timing));
    foreach (var t in EnumWire.Values<AlertType>()) Assert.Equal(t, r.For(t).Type);
  }

  [Fact, Trait("Scenario", "D-POLICY-02")]
  public void Missing_or_duplicate_policy_throws()
  {
    Assert.Throws<InvalidOperationException>(() => new AlertPolicyResolver(AlertPolicyResolver.DefaultPolicies().Skip(1)));
    Assert.Throws<InvalidOperationException>(() => new AlertPolicyResolver(AlertPolicyResolver.DefaultPolicies().Append(new CrashPolicy())));
  }

  private sealed class ProbePolicy : IAlertTypePolicy
  {
    public AlertType Type => AlertType.OffRoute; public bool NotifiesContacts => true;
    public TimeSpan? RepeatInterval(AlertTiming t) => TimeSpan.FromSeconds(1); public string ReasonText => "probe";
  }

  [Fact, Trait("Scenario", "D-POLICY-03")]
  public void Open_closed_probe_replace_a_policy_without_touching_production_code()
  {
    var r = new AlertPolicyResolver(AlertPolicyResolver.DefaultPolicies().Where(p => p.Type != AlertType.OffRoute).Append(new ProbePolicy()));
    Assert.Equal("probe", r.For(AlertType.OffRoute).ReasonText);
  }

  [Fact, Trait("Scenario", "D-POLICY-04")]
  public void Reasons_match_legacy()
  {
    var r = AlertPolicyResolver.CreateDefault();
    Assert.Equal("nghi gặp va chạm/té ngã và không phản hồi", r.For(AlertType.Crash).ReasonText);
    Assert.Equal("vừa bấm nút SOS", r.For(AlertType.Sos).ReasonText);
    Assert.Equal("mất tín hiệu quá lâu", r.For(AlertType.LostSignal).ReasonText);
    foreach (var t in EnumWire.Values<AlertType>()) Assert.False(string.IsNullOrWhiteSpace(r.For(t).ReasonText));
  }
}

public class NotificationDeliveryTests
{
  private static NotificationDelivery Q() => NotificationDelivery.Queue(Guid.NewGuid(), Guid.NewGuid(), DeliveryChannel.Sms, NotificationKind.Initial, 0, Make.Now);
  private static readonly TimeSpan Lease = TimeSpan.FromSeconds(60);
  private static NotificationDelivery Processing() { var d = Q(); d.Claim("w", Make.Now, Lease).ThrowIfFailed(); return d; }

  [Fact, Trait("Scenario", "D-DELIV-01")]
  public void Queue() { var d = Q(); Assert.Equal(DeliveryStatus.Queued, d.Status); Assert.Equal(0, d.Attempts); Assert.Equal(Make.Now, d.NextAttemptAt); }

  [Fact, Trait("Scenario", "D-DELIV-02")]
  public void Skip_is_terminal()
  {
    var d = Q(); d.Skip("dry-run", Make.Now).ThrowIfFailed();
    Assert.Equal(DeliveryStatus.Skipped, d.Status); Assert.Equal("dry-run", d.ProviderMessageId);
    d.Claim("w", Make.Now.AddDays(1), Lease).ShouldFailWith("DELIVERY_INVALID_STATE");
  }

  [Fact, Trait("Scenario", "D-DELIV-03")]
  public void Claim_rules()
  {
    var notDue = Q(); notDue.Claim("w", Make.Now.AddSeconds(-1), Lease).ShouldFailWith("DELIVERY_INVALID_STATE");
    var d = Q(); d.Claim("w", Make.Now, Lease).ThrowIfFailed();
    Assert.Equal(DeliveryStatus.Processing, d.Status); Assert.Equal(1, d.Attempts); Assert.Equal(Make.Now + Lease, d.LockedUntil);
    d.Claim("w2", Make.Now.AddSeconds(30), Lease).ShouldFailWith("DELIVERY_INVALID_STATE");
    d.Claim("w2", Make.Now.AddSeconds(61), Lease).ThrowIfFailed();
    Assert.Equal(2, d.Attempts); Assert.Equal("w2", d.LockedBy);
  }

  [Fact, Trait("Scenario", "D-DELIV-04")]
  public void MarkSent()
  {
    Q().MarkSent("id", Make.Now).ShouldFailWith("DELIVERY_INVALID_STATE");
    var d = Processing(); d.MarkSent("id", Make.Now.AddSeconds(1)).ThrowIfFailed();
    Assert.Equal(DeliveryStatus.Sent, d.Status); Assert.Equal(Make.Now.AddSeconds(1), d.SentAt); Assert.Equal("id", d.ProviderMessageId);
  }

  [Fact, Trait("Scenario", "D-DELIV-05")]
  public void Failure_backoff_15_30_60()
  {
    var d = Q(); var now = Make.Now; var expected = new[] { 15, 30, 60 };
    for (var i = 0; i < 3; i++)
    {
      d.Claim("w", now, Lease).ThrowIfFailed();
      d.MarkFailed("boom", now, DeliveryRetryPolicy.Default).ThrowIfFailed();
      Assert.Equal(DeliveryStatus.Queued, d.Status); Assert.Equal(now.AddSeconds(expected[i]), d.NextAttemptAt);
      now = d.NextAttemptAt;
    }
  }

  [Fact, Trait("Scenario", "D-DELIV-06")]
  public void Final_failure()
  {
    var d = Q(); var now = Make.Now;
    for (var i = 0; i < 4; i++) { d.Claim("w", now, Lease).ThrowIfFailed(); d.MarkFailed(new string('x', 600), now, DeliveryRetryPolicy.Default); now = d.NextAttemptAt; }
    Assert.Equal(DeliveryStatus.Failed, d.Status); Assert.Equal(500, d.Error!.Length);
  }

  [Fact, Trait("Scenario", "D-DELIV-07")]
  public void Terminal_states_cannot_transition()
  {
    var sent = Processing(); sent.MarkSent("x", Make.Now);
    sent.Claim("w", Make.Now.AddDays(1), Lease).ShouldFailWith("DELIVERY_INVALID_STATE");
    sent.MarkFailed("e", Make.Now, DeliveryRetryPolicy.Default).ShouldFailWith("DELIVERY_INVALID_STATE");
    var skipped = Q(); skipped.Skip("s", Make.Now);
    skipped.MarkSent("x", Make.Now).ShouldFailWith("DELIVERY_INVALID_STATE");
    var failed = Q(); var now = Make.Now;
    for (var i = 0; i < 4; i++) { failed.Claim("w", now, Lease); failed.MarkFailed("e", now, DeliveryRetryPolicy.Default); now = failed.NextAttemptAt; }
    failed.Claim("w", now.AddDays(1), Lease).ShouldFailWith("DELIVERY_INVALID_STATE");
  }
}
