using System;
using System.Linq;
using RoadMate.Domain.Events;
using RoadMate.Domain.Trips;
using RoadMate.Tests.TestKit;

namespace RoadMate.Tests.Domain;

public class TripLifecycleTests
{
  private static DateTimeOffset T0 => Make.Now;

  [Fact, Trait("Scenario", "D-TRIP-01")]
  public void Start_with_live_share()
  {
    var t = Make.DraftTrip(liveShare: true); t.Start(T0).ThrowIfFailed();
    Assert.Equal(TripStatus.Active, t.Status); Assert.Equal(T0, t.StartTime); Assert.Equal(32, t.TrackingToken!.Length); Assert.Null(t.TrackingExpiresAt);
  }

  [Fact, Trait("Scenario", "D-TRIP-02")]
  public void Start_keeps_existing_token()
  {
    var t = Make.DraftTrip(); t.RotateTrackingToken(); var tok = t.TrackingToken;
    t.Start(T0).ThrowIfFailed(); Assert.Equal(tok, t.TrackingToken);
  }

  [Fact, Trait("Scenario", "D-TRIP-03")]
  public void Start_requires_draft()
  {
    var t = Make.ActiveTrip(); t.Start(T0).ShouldFailWith("TRIP_NOT_DRAFT");
    t.Finish(T0, TimeSpan.Zero); t.Start(T0).ShouldFailWith("TRIP_NOT_DRAFT");
  }

  [Fact, Trait("Scenario", "D-TRIP-04")]
  public void Start_past_return_time() { Make.DraftTrip().Start(T0, T0).ShouldFailWith("INVALID_RETURN_TIME"); Make.DraftTrip().Start(T0, T0.AddHours(-1)).ShouldFailWith("INVALID_RETURN_TIME"); }

  [Fact, Trait("Scenario", "D-TRIP-05")]
  public void Finish_with_token()
  {
    var t = Make.ActiveTrip(liveShare: true); t.SetRest(30, T0); t.MarkLateReturnReminded(T0);
    t.Finish(T0.AddHours(2), TimeSpan.FromHours(2)).ThrowIfFailed();
    Assert.Equal(TripStatus.Finished, t.Status); Assert.Equal(T0.AddHours(2), t.EndTime); Assert.Null(t.RestUntil); Assert.Null(t.LateReturnRemindedAt);
    Assert.Equal(T0.AddHours(4), t.TrackingExpiresAt); Assert.Single(t.DomainEvents.OfType<TripFinished>());
  }

  [Fact, Trait("Scenario", "D-TRIP-06")]
  public void Finish_without_token() { var t = Make.ActiveTrip(); t.Finish(T0, TimeSpan.FromHours(2)); Assert.Null(t.TrackingExpiresAt); }

  [Fact, Trait("Scenario", "D-TRIP-07")]
  public void Finish_requires_active()
  {
    Make.DraftTrip().Finish(T0, TimeSpan.Zero).ShouldFailWith("TRIP_NOT_ACTIVE");
    var t = Make.ActiveTrip(); t.Finish(T0, TimeSpan.Zero); t.Finish(T0, TimeSpan.Zero).ShouldFailWith("TRIP_NOT_ACTIVE");
  }

  [Theory, Trait("Scenario", "D-TRIP-08")]
  [InlineData(4, false), InlineData(5, true), InlineData(480, true), InlineData(481, false)]
  public void Rest_bounds(int minutes, bool ok)
  {
    var t = Make.ActiveTrip(); var r = t.SetRest(minutes, T0);
    if (ok) { r.ThrowIfFailed(); Assert.Equal(T0.AddMinutes(minutes), t.RestUntil); } else r.ShouldFailWith("INVALID_REST");
  }

  [Fact, Trait("Scenario", "D-TRIP-09")]
  public void Not_active_guards()
  {
    var t = Make.DraftTrip();
    t.SetRest(30, T0).ShouldFailWith("TRIP_NOT_ACTIVE"); t.ClearRest().ShouldFailWith("TRIP_NOT_ACTIVE"); t.ExtendExpectedReturn(T0.AddHours(1), T0).ShouldFailWith("TRIP_NOT_ACTIVE");
  }

  [Fact, Trait("Scenario", "D-TRIP-10")]
  public void Extend_expected_return()
  {
    var t = Make.ActiveTrip(); t.MarkLateReturnReminded(T0);
    t.ExtendExpectedReturn(T0.AddHours(3), T0).ThrowIfFailed();
    Assert.Equal(T0.AddHours(3), t.ExpectedReturnTime); Assert.Null(t.LateReturnRemindedAt); Assert.Single(t.DomainEvents.OfType<ExpectedReturnExtended>());
    t.ExtendExpectedReturn(T0.AddHours(-1), T0).ShouldFailWith("INVALID_RETURN_TIME");
  }

  [Theory, Trait("Scenario", "D-TRIP-11")]
  [InlineData(20, true), InlineData(21, false)]
  public void Waypoint_limit(int count, bool ok)
  {
    var t = Make.DraftTrip(); var specs = Enumerable.Range(0, count).Select(i => Make.Waypoint("W" + i)).ToList();
    var r = t.ReplaceWaypoints(specs);
    if (ok) { r.ThrowIfFailed(); Assert.Equal(Enumerable.Range(0, count), t.Waypoints.OrderBy(w => w.Seq).Select(w => w.Seq)); Assert.Equal("W3", t.Waypoints.Single(w => w.Seq == 3).Name); }
    else r.ShouldFailWith("TOO_MANY_WAYPOINTS");
  }

  [Fact, Trait("Scenario", "D-TRIP-12")]
  public void Edits_require_draft()
  {
    var t = Make.ActiveTrip();
    t.ReplaceWaypoints([]).ShouldFailWith("TRIP_NOT_DRAFT"); t.UpdateDetails(Make.Details()).ShouldFailWith("TRIP_NOT_DRAFT");
  }

  [Fact, Trait("Scenario", "D-TRIP-13")]
  public void Rotate_token()
  {
    var t = Make.ActiveTrip(liveShare: true); var old = t.TrackingToken;
    t.RotateTrackingToken().ThrowIfFailed(); Assert.NotEqual(old, t.TrackingToken); Assert.True(t.LiveShare); Assert.Null(t.TrackingExpiresAt);
    t.Finish(T0, TimeSpan.Zero); t.RotateTrackingToken().ShouldFailWith("TRIP_FINISHED");
  }

  [Fact, Trait("Scenario", "D-TRIP-14")]
  public void Disable_tracking() { var t = Make.ActiveTrip(liveShare: true); t.DisableTracking(); Assert.Null(t.TrackingToken); Assert.False(t.LiveShare); }

  [Fact, Trait("Scenario", "D-TRIP-15")]
  public void Recipients_diff()
  {
    var t = Make.DraftTrip(); Guid a = Guid.NewGuid(), b = Guid.NewGuid(), c = Guid.NewGuid();
    t.SetRecipients([a, b]); Assert.Equal(2, t.Recipients.Count);
    t.SetRecipients([b, c]); Assert.Equal(new[] { b, c }.OrderBy(x => x), t.Recipients.Select(r => r.ContactId).OrderBy(x => x));
    var ids = t.Recipients.Select(r => r.Id).ToList(); t.SetRecipients([b, c]);
    Assert.Equal(ids.OrderBy(x => x), t.Recipients.Select(r => r.Id).OrderBy(x => x));
  }

  [Fact, Trait("Scenario", "D-TRIP-16")]
  public void Attach_group_to_finished() { var t = Make.ActiveTrip(); t.Finish(T0, TimeSpan.Zero); t.AttachGroup(Guid.NewGuid()).ShouldFailWith("TRIP_FINISHED"); }
}

public class TripLiveTests
{
  private static DateTimeOffset T0 => Make.Now;

  [Fact, Trait("Scenario", "D-LIVE-01")]
  public void First_position_applies()
  {
    var t = Make.ActiveTrip();
    Assert.True(t.ApplyLivePosition(Make.Point(1, 2), 30, 90, true, T0, T0));
    Assert.NotNull(t.Live); Assert.Single(t.DomainEvents.OfType<LocationReported>());
  }

  [Fact, Trait("Scenario", "D-LIVE-02")]
  public void Older_position_is_ignored_but_refreshes_receive_clock()
  {
    var t = Make.ActiveTrip(); t.ApplyLivePosition(Make.Point(1, 2), 30, 90, true, T0, T0);
    Assert.False(t.ApplyLivePosition(Make.Point(9, 9), 10, 10, true, T0.AddSeconds(-5), T0.AddSeconds(20)));
    Assert.Equal(1, t.Live!.Lat); Assert.Equal(T0.AddSeconds(20), t.Live.UpdatedAt);
  }

  [Fact, Trait("Scenario", "D-LIVE-03")]
  public void Equal_reportedAt_is_applied()
  {
    var t = Make.ActiveTrip(); t.ApplyLivePosition(Make.Point(1, 2), 30, 90, true, T0, T0);
    Assert.True(t.ApplyLivePosition(Make.Point(5, 6), 30, 90, true, T0, T0.AddSeconds(1)));
    Assert.Equal(5, t.Live!.Lat);
  }

  [Fact, Trait("Scenario", "D-LIVE-04")]
  public void Null_battery_keeps_previous()
  {
    var t = Make.ActiveTrip(); t.ApplyLivePosition(Make.Point(1, 2), 30, 77, true, T0, T0);
    t.ApplyLivePosition(Make.Point(1, 2), 30, null, true, T0.AddSeconds(5), T0.AddSeconds(5));
    Assert.Equal(77, t.Live!.Battery);
  }

  [Fact, Trait("Scenario", "D-LIVE-05")]
  public void Lost_signal_flag_is_reported_then_cleared_explicitly()
  {
    var t = Make.ActiveTrip(); t.MarkLostSignalRaised(T0);
    t.ApplyLivePosition(Make.Point(1, 2), 30, 77, true, T0, T0);
    Assert.True(t.DomainEvents.OfType<LocationReported>().Single().ClearsLostSignal);
    Assert.True(t.LostSignalAlertOpen); t.MarkSignalRestored(); Assert.False(t.LostSignalAlertOpen);
  }
}

public class TripStatisticsTests
{
  private static TripPointSample P(double lat, double lng, int sec, double? speed = null, double? alt = null) => new(lat, lng, speed, alt, null, Make.Now.AddSeconds(sec));
  // ~1 km north per 0.009 degrees of latitude
  private const double KmLat = 0.0089932;

  [Fact, Trait("Scenario", "D-STATS-01")]
  public void Empty_and_single() { Assert.Equal(0, TripStatistics.Compute([], 0).DistanceKm); var s = TripStatistics.Compute([P(1, 1, 0)], 0); Assert.Equal(0, s.DistanceKm); Assert.Equal(0, s.MovingMin); Assert.Equal(0, s.AvgSpeed); }

  [Fact, Trait("Scenario", "D-STATS-02")]
  public void One_km_in_a_minute()
  {
    var s = TripStatistics.Compute([P(10, 106, 0), P(10 + KmLat, 106, 60)], 0);
    Assert.Equal(1.0, s.DistanceKm, 1); Assert.Equal(1, s.MovingMin); Assert.Equal(60, s.AvgSpeed, 0);
  }

  [Fact, Trait("Scenario", "D-STATS-03")]
  public void Slow_segments_are_rest() { var s = TripStatistics.Compute([P(10, 106, 0), P(10, 106, 180, 0)], 0); Assert.Equal(3, s.RestMin); Assert.Equal(0, s.MovingMin); }

  [Fact, Trait("Scenario", "D-STATS-04")]
  public void Stops_counted_once_per_continuous_stop()
  {
    var s = TripStatistics.Compute([P(10, 106, 0, 30), P(10, 106, 60, 0), P(10, 106, 130, 0), P(10, 106, 200, 0), P(10.01, 106, 260, 40), P(10.01, 106, 400, 0)], 0);
    Assert.Equal(2, s.StopCount);
  }

  [Fact, Trait("Scenario", "D-STATS-05")]
  public void Non_positive_dt_skipped() { var s = TripStatistics.Compute([P(10, 106, 10), P(11, 107, 10), P(12, 108, 5)], 0); Assert.Equal(0, s.DistanceKm); }

  [Fact, Trait("Scenario", "D-STATS-06"), Trait("Scenario", "D-STATS-GAP-01")]
  public void Gap_counts_in_neither_distance_nor_time()
  {
    var s = TripStatistics.Compute([P(10, 106, 0, 60), P(10 + KmLat, 106, 60, 60), P(11, 106, 60 + 1200, 60), P(11 + KmLat, 106, 60 + 1260, 60)], 0);
    Assert.Equal(2.0, s.DistanceKm, 1); Assert.Equal(2, s.MovingMin); Assert.True(s.AvgSpeed <= s.MaxSpeed);
    Assert.Equal(60, s.AvgSpeed, 0);
  }

  [Fact, Trait("Scenario", "D-STATS-07")]
  public void Null_values_are_zero() { var s = TripStatistics.Compute([P(10, 106, 0), P(10, 106, 10)], 0); Assert.Equal(0, s.MaxSpeed); Assert.Equal(0, s.MaxAltitude); }

  [Fact, Trait("Scenario", "D-STATS-08")]
  public void Rounding_and_alert_count()
  {
    var s = TripStatistics.Compute([P(10, 106, 0, 50, 120.5), P(10.0123456, 106, 100, 55, 300.25)], 3);
    Assert.Equal(Math.Round(s.DistanceKm, 2), s.DistanceKm); Assert.Equal(Math.Round(s.AvgSpeed, 1), s.AvgSpeed);
    Assert.Equal(3, s.AlertCount); Assert.Equal(55, s.MaxSpeed); Assert.Equal(300.25, s.MaxAltitude);
  }
}
