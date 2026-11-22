using System;
using System.Collections.Generic;
using RoadMate.Domain.Alerts;
using RoadMate.Domain.Common;
using RoadMate.Domain.Trips;
using RoadMate.Domain.ValueObjects;

namespace RoadMate.Tests.TestKit;

/// <summary>Builders for domain objects (valid by default; override what the test is about).</summary>
public static class Make
{
  public static readonly DateTimeOffset Now = new(2026, 7, 1, 8, 0, 0, TimeSpan.Zero);
  public static GeoPoint Point(double lat = 10.7769, double lng = 106.7009) => GeoPoint.Create(lat, lng).Value;
  public static PhoneNumber Phone(string raw = "0912345678") => PhoneNumber.ParseVietnam(raw).Value;
  public static ClientRequestId ClientId(string v = "client-1") => ClientRequestId.Create(v).Value;

  public static TripDetails Details(bool liveShare = false, DateTimeOffset? expectedReturn = null) =>
    new("Sài Gòn - Đà Lạt", "Sài Gòn", Point(), "Đà Lạt", Point(11.9404, 108.4583), null, 300, 480, false, false, expectedReturn, null, null, liveShare);

  public static Trip DraftTrip(Guid? owner = null, bool liveShare = false, IReadOnlyList<WaypointSpec>? waypoints = null) =>
    Trip.Create(owner ?? Guid.NewGuid(), Details(liveShare), waypoints ?? [], []).Value;

  public static Trip ActiveTrip(Guid? owner = null, bool liveShare = false, DateTimeOffset? now = null)
  {
    var t = DraftTrip(owner, liveShare);
    t.Start(now ?? Now).ThrowIfFailed();
    return t;
  }

  public static WaypointSpec Waypoint(string name = "Quán A", int? stay = null) => new(name, Point(11, 107), stay);

  public static global::RoadMate.Domain.Alerts.Alert Alert(AlertType type = AlertType.Crash, Guid? tripId = null, Guid? userId = null, SimulationMode? sim = null, DateTimeOffset? now = null, string clientId = "c-1")
  {
    var policy = AlertPolicyResolver.CreateDefault().For(type);
    var timing = new AlertTiming(TimeSpan.FromMinutes(2), TimeSpan.FromMinutes(10));
    return global::RoadMate.Domain.Alerts.Alert.Raise(tripId ?? Guid.NewGuid(), userId ?? Guid.NewGuid(), ClientId(clientId), type, Point(), 80, sim, policy, policy.RepeatInterval(timing), now ?? Now);
  }

  public static global::RoadMate.Domain.Users.User User(string uid = "uid-1", string name = "Nguyễn Văn An") => global::RoadMate.Domain.Users.User.Create(uid, "an@example.com", name);

  public static global::RoadMate.Domain.Groups.Group Group(Guid? leader = null, string name = "Đoàn Đà Lạt") =>
    global::RoadMate.Domain.Groups.Group.Create(leader ?? Guid.NewGuid(), name, null, null, null, null, "ABCDEF", Now).Value;

  public static T ThrowIfFailed<T>(this T r) where T : Result
  {
    if (r.IsFailure) throw new InvalidOperationException($"Unexpected failure {r.Error.Code}: {r.Error.Message}");
    return r;
  }

  public static void ShouldFailWith(this Result r, string code)
  {
    Assert.True(r.IsFailure, $"expected failure {code} but succeeded");
    Assert.Equal(code, r.Error.Code);
  }
}
