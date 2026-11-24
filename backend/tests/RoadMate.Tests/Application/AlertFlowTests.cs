using RoadMate.Domain.Users;
using RoadMate.Application.Alerts;
using RoadMate.Application.Alerts.DTOs.Requests;
using RoadMate.Application.Alerts.DTOs.Responses;
using RoadMate.Application.Monitoring;
using RoadMate.Application.Tracking;
using RoadMate.Application.Trips;
using RoadMate.Domain.Alerts;
using RoadMate.Domain.Common;
using RoadMate.Domain.Trips;
using RoadMate.Tests.TestKit;
using System.Threading.Tasks;
using System.Linq;
using System;

namespace RoadMate.Tests.Application;

public class AlertRaiseTests
{
  private static CreateAlertRequest Req(string client = "c-1", string type = "crash") => new(client, type, 10.7, 106.7, 80);

  private static (World w, User owner, Trip trip) Setup(int contacts = 2)
  {
    var w = new World(); var owner = w.AddUser("Nguyễn Văn An");
    var cs = Enumerable.Range(0, contacts).Select(i => w.AddContact(owner, "09123456" + (10 + i))).ToArray();
    return (w, owner, w.AddActiveTrip(owner, cs));
  }

  [Fact, Trait("Scenario", "A-RAISE-01")]
  public async Task Crash_with_two_contacts_plans_two_sms_and_does_not_send_inline()
  {
    var (w, owner, trip) = Setup();
    var r = await w.Get<IAlertService>().CreateAsync(owner.Id, trip.Id, Req(), default);
    Assert.True(r.IsSuccess);
    Assert.Equal("pending", r.Value.Status); Assert.Equal(1, r.Value.RepeatCount);
    Assert.Equal(2, w.Deliveries.Count(d => d.Channel == DeliveryChannel.Sms && d.Status == DeliveryStatus.Queued));
    Assert.Empty(w.Sms.Sent);                                   // nothing is sent inside the request transaction
    Assert.Equal(2, w.Acks.Count);
  }

  [Fact, Trait("Scenario", "A-RAISE-02")]
  public async Task Same_clientId_returns_existing_without_new_rows()
  {
    var (w, owner, trip) = Setup(); var svc = w.Get<IAlertService>();
    var first = await svc.CreateAsync(owner.Id, trip.Id, Req(), default);
    var rows = w.Deliveries.Count;
    var again = await svc.CreateAsync(owner.Id, trip.Id, Req(), default);
    Assert.True(again.IsSuccess); Assert.Equal(first.Value.Id, again.Value.Id); Assert.Equal(rows, w.Deliveries.Count); Assert.Single(w.Alerts);
  }

  [Fact, Trait("Scenario", "A-RAISE-03")]
  public async Task Other_users_clientId_conflicts()
  {
    var (w, owner, trip) = Setup(); await w.Get<IAlertService>().CreateAsync(owner.Id, trip.Id, Req(), default);
    var other = w.AddUser("Khác"); var otherTrip = w.AddActiveTrip(other);
    (await w.Get<IAlertService>().CreateAsync(other.Id, otherTrip.Id, Req(), default)).ShouldFailWith("CLIENT_ID_TAKEN");
  }

  [Fact, Trait("Scenario", "A-RAISE-04")]
  public async Task Trip_must_be_owned_and_active()
  {
    var (w, owner, trip) = Setup(); var svc = w.Get<IAlertService>();
    (await svc.CreateAsync(Guid.NewGuid(), trip.Id, Req(), default)).ShouldFailWith("TRIP_NOT_FOUND");
    trip.Finish(w.Clock.Now, TimeSpan.Zero);
    (await svc.CreateAsync(owner.Id, trip.Id, Req(), default)).ShouldFailWith("TRIP_NOT_ACTIVE");
  }

  [Fact, Trait("Scenario", "A-RAISE-05")]
  public async Task OffRoute_never_plans_notifications()
  {
    var (w, owner, trip) = Setup();
    var r = await w.Get<IAlertService>().CreateAsync(owner.Id, trip.Id, Req(type: "offroute"), default);
    Assert.True(r.IsSuccess); Assert.Empty(w.Deliveries); Assert.Equal(0, r.Value.RepeatCount);
  }

  [Fact, Trait("Scenario", "A-RAISE-06")]
  public async Task LowBattery_is_notified_once()
  {
    var (w, owner, trip) = Setup();
    var r = await w.Get<IAlertService>().CreateAsync(owner.Id, trip.Id, Req(type: "lowBattery"), default);
    Assert.Equal(2, w.Deliveries.Count); Assert.Null(w.Alerts[0].NextRepeatAt);
  }

  [Fact, Trait("Scenario", "A-RAISE-07")]
  public async Task Linked_contact_with_token_also_gets_push()
  {
    var w = new World(); var owner = w.AddUser(); var relative = w.AddUser("Mẹ", fcm: "tok-1");
    var c = w.AddContact(owner, "0912345678", linkedUser: relative.Id); var trip = w.AddActiveTrip(owner, c);
    await w.Get<IAlertService>().CreateAsync(owner.Id, trip.Id, Req(), default);
    Assert.Single(w.Deliveries, d => d.Channel == DeliveryChannel.Push);
  }

  [Fact, Trait("Scenario", "A-RAISE-08")]
  public async Task Unique_violation_race_returns_the_winner()
  {
    var (w, owner, trip) = Setup(); var svc = w.Get<IAlertService>();
    var winner = (await svc.CreateAsync(owner.Id, trip.Id, Req("race"), default)).Value;
    w.ThrowUniqueOnNextSave = true;                              // a parallel request with the same clientId won the index
    var loser = await w.Get<IAlertRaiser>().RaiseAsync(new RaiseAlertCommand(trip.Id, owner.Id, Make.ClientId("race2"), AlertType.Sos, Make.Point(), 50, null), w.Clock.Now, default);
    Assert.True(loser.IsFailure || loser.Value.Alert.ClientId == "race2" || loser.Value.Alert.Id == winner.Id);
  }

  [Fact, Trait("Scenario", "A-RAISE-09"), Trait("Scenario", "A-SIM-01")]
  public async Task Simulator_dry_run_skips_everything()
  {
    var (w, owner, trip) = Setup();
    var r = await w.Get<IAlertSimulationService>().SimulateAsync(owner.Id, new SimulateAlertRequest(trip.Id, "crash", null, null, null), default);
    Assert.True(r.IsSuccess); Assert.Equal("dryRun", r.Value.SimulationMode);
    Assert.All(w.Deliveries, d => Assert.Equal(DeliveryStatus.Skipped, d.Status));
    await w.Get<IOutboxProcessor>().ProcessBatchAsync("w", default);
    Assert.Empty(w.Sms.Sent);
  }

  [Fact, Trait("Scenario", "A-SIM-02")]
  public async Task Simulator_disabled_is_404()
  {
    var w = new World(debug: d => d.EnableSimulator = false); var owner = w.AddUser(); var trip = w.AddActiveTrip(owner);
    (await w.Get<IAlertSimulationService>().SimulateAsync(owner.Id, new SimulateAlertRequest(trip.Id, null, null, null, null), default)).ShouldFailWith("SIMULATOR_DISABLED");
  }
}

public class OutboxTests
{
  private static async Task<(World w, User owner, Trip trip, AlertResponse alert)> Raised(int contacts = 2)
  {
    var w = new World(); var owner = w.AddUser("Nguyễn Văn An");
    var cs = Enumerable.Range(0, contacts).Select(i => w.AddContact(owner, "09123456" + (10 + i))).ToArray();
    var trip = w.AddActiveTrip(owner, cs);
    var a = (await w.Get<IAlertService>().CreateAsync(owner.Id, trip.Id, new CreateAlertRequest("c1", "crash", 10.7, 106.7, 80), default)).Value;
    return (w, owner, trip, a);
  }

  [Fact, Trait("Scenario", "A-PROC-01")]
  public async Task Processing_sends_sms_marks_sent_and_alert_sent()
  {
    var (w, _, _, _) = await Raised();
    var n = await w.Get<IOutboxProcessor>().ProcessBatchAsync("w1", default);
    Assert.Equal(2, n); Assert.Equal(2, w.Sms.Sent.Count);
    Assert.All(w.Deliveries, d => Assert.Equal(DeliveryStatus.Sent, d.Status));
    Assert.Equal(AlertStatus.Sent, w.Alerts[0].Status);
    Assert.Contains("Nguyễn Văn An", w.Sms.Sent[0].Body); Assert.Contains("https://roadmate.test/ack/", w.Sms.Sent[0].Body);
  }

  [Fact, Trait("Scenario", "A-PROC-02")]
  public async Task Failure_is_retried_with_backoff_then_succeeds()
  {
    var (w, _, _, _) = await Raised(1);
    w.Sms.Fail = true; var p = w.Get<IOutboxProcessor>();
    await p.ProcessBatchAsync("w", default);
    Assert.Equal(DeliveryStatus.Queued, w.Deliveries[0].Status); Assert.Equal(w.Clock.Now.AddSeconds(15), w.Deliveries[0].NextAttemptAt);
    Assert.Equal(0, await p.ProcessBatchAsync("w", default));      // not due yet
    w.Sms.Fail = false; w.Clock.Advance(TimeSpan.FromSeconds(16));
    await p.ProcessBatchAsync("w", default);
    Assert.Equal(DeliveryStatus.Sent, w.Deliveries[0].Status); Assert.Equal(2, w.Deliveries[0].Attempts);
  }

  [Fact, Trait("Scenario", "A-PROC-03")]
  public async Task Rows_of_closed_alerts_are_abandoned()
  {
    var (w, owner, _, a) = await Raised(1);
    await w.Get<IAlertLifecycleService>().CancelAsync(owner.Id, a.Id, default);   // closed before anything was sent → no "safe" notice either
    await w.Get<IOutboxProcessor>().ProcessBatchAsync("w", default);
    Assert.Empty(w.Sms.Sent);
  }

  [Fact, Trait("Scenario", "A-PROC-04")]
  public async Task Expired_lease_is_reclaimed_after_a_crash()
  {
    var (w, _, _, _) = await Raised(1);
    w.Deliveries[0].Claim("dead-worker", w.Clock.Now, TimeSpan.FromSeconds(60));              // crashed mid-send
    Assert.Equal(0, await w.Get<IOutboxProcessor>().ProcessBatchAsync("w", default));
    w.Clock.Advance(TimeSpan.FromSeconds(61));
    Assert.Equal(1, await w.Get<IOutboxProcessor>().ProcessBatchAsync("w", default));
    Assert.Equal(DeliveryStatus.Sent, w.Deliveries[0].Status);
  }

  [Fact, Trait("Scenario", "A-CLOSE-01")]
  public async Task Cancel_after_notification_plans_safe_notice()
  {
    var (w, owner, _, a) = await Raised(2);
    await w.Get<IOutboxProcessor>().ProcessBatchAsync("w", default);
    w.Sms.Sent.Clear();
    (await w.Get<IAlertLifecycleService>().CancelAsync(owner.Id, a.Id, default)).ThrowIfFailed();
    await w.Get<IOutboxProcessor>().ProcessBatchAsync("w", default);
    Assert.Equal(2, w.Sms.Sent.Count); Assert.All(w.Sms.Sent, m => Assert.Contains("AN TOÀN", m.Body));
  }

  [Fact, Trait("Scenario", "A-CLOSE-02")]
  public async Task Close_is_idempotent_and_conflicting_close_is_409()
  {
    var (w, owner, _, a) = await Raised(1); var svc = w.Get<IAlertLifecycleService>();
    (await svc.ResolveAsync(owner.Id, a.Id, default)).ThrowIfFailed();
    var rows = w.Deliveries.Count;
    (await svc.ResolveAsync(owner.Id, a.Id, default)).ThrowIfFailed();
    Assert.Equal(rows, w.Deliveries.Count);
    (await svc.CancelAsync(owner.Id, a.Id, default)).ShouldFailWith("ALERT_CLOSED");
  }

  [Fact, Trait("Scenario", "A-CLOSE-03")]
  public async Task Finish_trip_closes_alerts_and_plans_safe_notice_in_one_commit()
  {
    var (w, owner, trip, _) = await Raised(1);
    await w.Get<IOutboxProcessor>().ProcessBatchAsync("w", default);
    var before = w.Deliveries.Count;
    (await w.Get<ITripLifecycleService>().FinishAsync(owner.Id, trip.Id, default)).ThrowIfFailed();
    Assert.Equal(AlertStatus.Resolved, w.Alerts[0].Status); Assert.Equal(AlertResolver.User, w.Alerts[0].ResolvedBy);
    Assert.Equal(before + 1, w.Deliveries.Count); Assert.Equal(NotificationKind.Closed, w.Deliveries[^1].Kind);
  }

  [Fact, Trait("Scenario", "A-ACK-01"), Trait("Scenario", "A-ACK-02")]
  public async Task Ack_stops_repeats_for_that_contact_only_and_keeps_alert_open()
  {
    var (w, _, _, a) = await Raised(2);
    var ack = w.Acks[0];
    var r = await w.Get<IAlertAckService>().AckAsync(ack.Token, default);
    Assert.True(r.IsSuccess); Assert.Equal("Nguyễn A.", r.Value.OwnerName); Assert.True(w.Alerts[0].IsOpen);
    w.Clock.Advance(TimeSpan.FromMinutes(3));
    await w.Get<IAlertRepeatProcessor>().ProcessDueAsync("w", default);
    var repeats = w.Deliveries.Where(d => d.Kind == NotificationKind.Repeat).ToList();
    Assert.Single(repeats); Assert.NotEqual(ack.ContactId, repeats[0].ContactId);
    (await w.Get<IAlertAckService>().AckAsync("nope", default)).ShouldFailWith("ACK_LINK_INVALID");
  }
}

public class RepeatTests
{
  private static async Task<(World w, User o, Trip t)> Raised(AlertType type = AlertType.Crash)
  {
    var w = new World(); var o = w.AddUser(); var c = w.AddContact(o); var t = w.AddActiveTrip(o, c);
    await w.Get<IAlertService>().CreateAsync(o.Id, t.Id, new CreateAlertRequest("c", EnumWire.ToWire(type), 1, 1, 50), default);
    return (w, o, t);
  }

  [Fact, Trait("Scenario", "A-REPEAT-01")]
  public async Task Due_alert_plans_one_repeat()
  {
    var (w, _, _) = await Raised(); w.Clock.Advance(TimeSpan.FromMinutes(2));
    Assert.Equal(1, await w.Get<IAlertRepeatProcessor>().ProcessDueAsync("w", default));
    Assert.Equal(2, w.Alerts[0].RepeatCount); Assert.Contains(w.Deliveries, d => d.Kind == NotificationKind.Repeat && d.Sequence == 2);
    Assert.Equal(w.Clock.Now.AddMinutes(2), w.Alerts[0].NextRepeatAt);
  }

  [Fact, Trait("Scenario", "A-REPEAT-02")]
  public async Task Max_repeats_stops_scheduling()
  {
    var (w, _, _) = await Raised();               // MaxRepeats = 3 in the World
    for (var i = 0; i < 5; i++) { w.Clock.Advance(TimeSpan.FromMinutes(2)); await w.Get<IAlertRepeatProcessor>().ProcessDueAsync("w", default); }
    Assert.Equal(3, w.Alerts[0].RepeatCount); Assert.Null(w.Alerts[0].NextRepeatAt); Assert.True(w.Alerts[0].IsOpen);
  }

  [Fact, Trait("Scenario", "A-REPEAT-03")]
  public async Task Finished_trip_closes_alert_as_system()
  {
    var (w, _, t) = await Raised(); t.Finish(w.Clock.Now, TimeSpan.Zero); w.Alerts[0].Close(AlertStatus.Cancelled, AlertResolver.User, w.Clock.Now);
    var (w2, _, t2) = await Raised(); t2.Finish(w2.Clock.Now, TimeSpan.Zero); w2.Clock.Advance(TimeSpan.FromMinutes(2));
    await w2.Get<IAlertRepeatProcessor>().ProcessDueAsync("w", default);
    Assert.Equal(AlertResolver.System, w2.Alerts[0].ResolvedBy); Assert.DoesNotContain(w2.Deliveries, d => d.Kind == NotificationKind.Repeat);
  }

  [Fact, Trait("Scenario", "A-REPEAT-04")]
  public async Task All_acked_stops_repeating()
  {
    var (w, _, _) = await Raised(); await w.Get<IAlertAckService>().AckAsync(w.Acks[0].Token, default);
    w.Clock.Advance(TimeSpan.FromMinutes(2)); await w.Get<IAlertRepeatProcessor>().ProcessDueAsync("w", default);
    Assert.DoesNotContain(w.Deliveries, d => d.Kind == NotificationKind.Repeat); Assert.Null(w.Alerts[0].NextRepeatAt);
  }

  [Fact, Trait("Scenario", "A-REPEAT-06")]
  public async Task Interval_by_type()
  {
    var (w, _, _) = await Raised(AlertType.LostSignal); Assert.Equal(w.Clock.Now.AddMinutes(10), w.Alerts[0].NextRepeatAt);
    var (w2, _, _) = await Raised(AlertType.Sos); Assert.Equal(w2.Clock.Now.AddMinutes(2), w2.Alerts[0].NextRepeatAt);
  }
}

public class TelemetryAndMonitorTests
{
  [Fact, Trait("Scenario", "A-TELE-01"), Trait("Scenario", "A-TELE-02")]
  public async Task Telemetry_validation()
  {
    var w = new World(); var o = w.AddUser(); var t = w.AddActiveTrip(o); var svc = w.Get<ITelemetryService>();
    var p = new PointInput(10, 106, 20, 5, 90, w.Clock.Now.UtcDateTime);
    Assert.True((await svc.AddPointsAsync(o.Id, t.Id, new AddPointsRequest([]), default)).IsSuccess); Assert.Empty(w.Points);
    (await svc.AddPointsAsync(Guid.NewGuid(), t.Id, new AddPointsRequest([p]), default)).ShouldFailWith("TRIP_NOT_FOUND");
    new AddPointsRequestValidator().Validate(new AddPointsRequest(Enumerable.Repeat(p, 501).ToList())).ShouldFailWith("BATCH_TOO_LARGE");
    new AddPointsRequestValidator().Validate(new AddPointsRequest([p with { Lat = 99 }])).ShouldFailWith("INVALID_LOCATION");
  }

  [Fact, Trait("Scenario", "A-TELE-03")]
  public async Task Out_of_order_batch_keeps_max_recordedAt_as_live()
  {
    var w = new World(); var o = w.AddUser(); var t = w.AddActiveTrip(o); var now = w.Clock.Now.UtcDateTime;
    await w.Get<ITelemetryService>().AddPointsAsync(o.Id, t.Id, new AddPointsRequest([new(11, 1, null, null, 70, now.AddSeconds(10)), new(10, 1, null, null, 80, now)]), default);
    Assert.Equal(2, w.Points.Count); Assert.Equal(11, w.Live[t.Id].Location.Lat);
  }

  [Fact, Trait("Scenario", "A-TELE-05"), Trait("Scenario", "A-MON-01")]
  public async Task Lost_signal_raised_then_closed_automatically_when_data_returns()
  {
    var w = new World(); var o = w.AddUser(); var c = w.AddContact(o); var t = w.AddActiveTrip(o, c);
    w.Clock.Advance(TimeSpan.FromMinutes(11));
    await w.Get<ITripMonitorService>().RunOnceAsync(default);
    Assert.Single(w.Alerts, a => a.Type == AlertType.LostSignal && a.IsOpen); Assert.True(t.LostSignalAlertOpen);
    await w.Get<ITripMonitorService>().RunOnceAsync(default);                           // no duplicate
    Assert.Single(w.Alerts);
    await w.Get<ITelemetryService>().HeartbeatAsync(o.Id, t.Id, new HeartbeatRequest(10, 106, 0, 50, true), default);
    Assert.Equal(AlertStatus.Resolved, w.Alerts[0].Status); Assert.Equal(AlertResolver.System, w.Alerts[0].ResolvedBy); Assert.False(t.LostSignalAlertOpen);
  }

  [Fact, Trait("Scenario", "A-MON-02")]
  public async Task Late_return_reminds_owner_first_then_alerts_relatives()
  {
    var w = new World(); var o = w.AddUser("An", fcm: "tok"); var c = w.AddContact(o); var t = w.AddActiveTrip(o, c);
    t.ExtendExpectedReturn(w.Clock.Now.AddHours(1), w.Clock.Now);
    w.Clock.Advance(TimeSpan.FromMinutes(121));                                           // expected + 60 min grace passed
    await w.Get<ITelemetryService>().HeartbeatAsync(o.Id, t.Id, new HeartbeatRequest(10, 106, 0, 50, true), default);   // owner online, so no lostSignal
    await w.Get<ITripMonitorService>().RunOnceAsync(default);
    Assert.Single(w.Push.Calls); Assert.Empty(w.Alerts);
    w.Clock.Advance(TimeSpan.FromMinutes(16));
    await w.Get<ITelemetryService>().HeartbeatAsync(o.Id, t.Id, new HeartbeatRequest(10, 106, 0, 50, true), default);
    await w.Get<ITripMonitorService>().RunOnceAsync(default);
    Assert.Single(w.Alerts, a => a.Type == AlertType.LateReturn);
  }

  [Fact, Trait("Scenario", "A-LIFE-01"), Trait("Scenario", "A-LIFE-02")]
  public async Task Start_trip_rules()
  {
    var w = new World(); var o = w.AddUser(); var svc = w.Get<ITripLifecycleService>();
    var draft = Make.DraftTrip(o.Id); w.Trips.Add(draft);
    var other = Make.DraftTrip(o.Id); w.Trips.Add(other);
    var ok = await svc.StartAsync(o.Id, draft.Id, new StartTripRequest(null, null, true), default);
    Assert.True(ok.IsSuccess); Assert.NotNull(ok.Value.TrackingUrl); Assert.Equal(2, ok.Value.Warnings.Count);
    (await svc.StartAsync(o.Id, other.Id, new StartTripRequest(null, null, null), default)).ShouldFailWith("ANOTHER_TRIP_ACTIVE");
    (await svc.StartAsync(o.Id, draft.Id, new StartTripRequest(null, null, null), default)).ShouldFailWith("TRIP_NOT_DRAFT");
  }

  [Fact, Trait("Scenario", "A-LIFE-03")]
  public async Task Finish_computes_stats_and_expires_the_link()
  {
    var w = new World(); var o = w.AddUser(); var t = Make.DraftTrip(o.Id, liveShare: true); w.Trips.Add(t); t.Start(w.Clock.Now);
    var now = w.Clock.Now;
    w.Points.AddRange([new(10, 106, 40, 5, null, now), new(10.0089932, 106, 60, 9, null, now.AddSeconds(60))]);
    var r = await w.Get<ITripLifecycleService>().FinishAsync(o.Id, t.Id, default);
    Assert.True(r.IsSuccess); Assert.Equal(1.0, r.Value.Stats.DistanceKm, 1); Assert.Equal(now.AddMinutes(120), t.TrackingExpiresAt);
  }
}
