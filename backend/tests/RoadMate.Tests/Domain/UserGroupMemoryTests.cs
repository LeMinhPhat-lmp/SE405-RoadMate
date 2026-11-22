using System;
using System.Linq;
using RoadMate.Domain.Events;
using RoadMate.Domain.Groups;
using RoadMate.Domain.Trips;
using RoadMate.Domain.Users;
using RoadMate.Tests.TestKit;

namespace RoadMate.Tests.Domain;

public class UserTests
{
  [Fact, Trait("Scenario", "D-USER-01")]
  public void Five_contacts_default_priorities()
  {
    var u = Make.User();
    for (var i = 1; i <= 5; i++) { var c = u.AddContact("C" + i, Make.Phone("09123456" + (10 + i)), null, null, null); Assert.Equal(i, c.Value.Priority); }
  }

  [Fact, Trait("Scenario", "D-USER-02")]
  public void Sixth_contact_rejected()
  {
    var u = Make.User(); for (var i = 0; i < 5; i++) u.AddContact("C", Make.Phone(), null, null, null);
    u.AddContact("C6", Make.Phone(), null, null, null).ShouldFailWith("CONTACT_LIMIT");
  }

  [Fact, Trait("Scenario", "D-USER-03")]
  public void Explicit_priority() => Assert.Equal(9, Make.User().AddContact("C", Make.Phone(), null, 9, null).Value.Priority);

  [Fact, Trait("Scenario", "D-USER-04")]
  public void Invalid_input()
  {
    Make.User().AddContact("  ", Make.Phone(), null, null, null).ShouldFailWith("INVALID_NAME");
    RoadMate.Domain.ValueObjects.PhoneNumber.ParseVietnam("123").ShouldFailWith("INVALID_PHONE");
  }

  [Fact, Trait("Scenario", "D-USER-05")]
  public void Removing_frees_slot()
  {
    var u = Make.User(); var first = u.AddContact("C0", Make.Phone(), null, null, null).Value;
    for (var i = 0; i < 4; i++) u.AddContact("C", Make.Phone(), null, null, null);
    u.RemoveContact(first.Id, Guid.NewGuid(), Make.Now).ThrowIfFailed(); Assert.True(first.IsDeleted);
    Assert.True(u.AddContact("new", Make.Phone(), null, null, null).IsSuccess);
    u.RemoveContact(Guid.NewGuid(), Guid.NewGuid(), Make.Now).ShouldFailWith("CONTACT_NOT_FOUND");
  }

  [Theory, Trait("Scenario", "D-USER-06")]
  [InlineData(0, false), InlineData(1, true), InlineData(240, true), InlineData(241, false)]
  public void Idle_minutes(int v, bool ok)
  {
    var r = Make.User().UpdateProfile(new ProfileUpdate(null, null, null, null, null, null, v, null, null, null, null));
    if (ok) Assert.True(r.IsSuccess); else r.ShouldFailWith("INVALID_IDLE");
  }

  [Fact, Trait("Scenario", "D-USER-07")]
  public void Auth_sync()
  {
    var u = Make.User(name: "An"); u.ApplyAuthSync("Khác", "fcm-1", "+84912345678");
    Assert.Equal("An", u.Name); Assert.Equal("fcm-1", u.FcmToken); Assert.Equal("+84912345678", u.Phone);
    var blank = Make.User(name: ""); blank.ApplyAuthSync("Bình", null, null); Assert.Equal("Bình", blank.Name);
  }
}

public class GroupTests
{
  private static DateTimeOffset T0 => Make.Now;

  [Fact, Trait("Scenario", "D-GROUP-01")]
  public void Create() { var l = Guid.NewGuid(); var g = Make.Group(l); Assert.Equal(GroupStatus.Open, g.Status); Assert.True(g.IsMember(l)); Assert.Equal(GroupRole.Leader, g.MemberOf(l)!.Role); Assert.Equal(6, g.Code.Length); }

  [Fact, Trait("Scenario", "D-GROUP-02")]
  public void Join_twice() { var g = Make.Group(); var u = Guid.NewGuid(); g.Join(u, T0, 10); Assert.True(g.Join(u, T0, 10).IsSuccess); Assert.Equal(2, g.Members.Count); }

  [Fact, Trait("Scenario", "D-GROUP-03")]
  public void Join_closed_and_full()
  {
    var closed = Make.Group(); var leader = closed.LeaderId; closed.Close(leader, T0); closed.Join(Guid.NewGuid(), T0, 10).ShouldFailWith("GROUP_CLOSED");
    var g = Make.Group(); for (var i = 0; i < 8; i++) g.Join(Guid.NewGuid(), T0, 10);
    Assert.Equal(9, g.Members.Count); Assert.True(g.Join(Guid.NewGuid(), T0, 10).IsSuccess);
    g.Join(Guid.NewGuid(), T0, 10).ShouldFailWith("GROUP_FULL");
  }

  [Fact, Trait("Scenario", "D-GROUP-04")]
  public void Leader_leaves_oldest_member_takes_over()
  {
    var g = Make.Group(); var leader = g.LeaderId; Guid a = Guid.NewGuid(), b = Guid.NewGuid();
    g.Join(a, T0.AddMinutes(1), 10); g.Join(b, T0.AddMinutes(2), 10);
    g.Leave(leader, T0.AddMinutes(3)).ThrowIfFailed();
    Assert.Equal(a, g.LeaderId); Assert.Equal(GroupRole.Leader, g.MemberOf(a)!.Role); Assert.False(g.IsMember(leader));
  }

  [Fact, Trait("Scenario", "D-GROUP-05")]
  public void Last_member_closes() { var g = Make.Group(); g.Leave(g.LeaderId, T0); Assert.Equal(GroupStatus.Closed, g.Status); }

  [Fact, Trait("Scenario", "D-GROUP-06")]
  public void Remove_member_rules()
  {
    var g = Make.Group(); var u = Guid.NewGuid(); g.Join(u, T0, 10);
    g.RemoveMember(u, g.LeaderId, T0).ShouldFailWith("NOT_GROUP_LEADER");
    g.RemoveMember(g.LeaderId, g.LeaderId, T0).ShouldFailWith("CANNOT_REMOVE_SELF");
    g.RemoveMember(g.LeaderId, Guid.NewGuid(), T0).ShouldFailWith("MEMBER_NOT_FOUND");
    g.RemoveMember(g.LeaderId, u, T0).ThrowIfFailed(); Assert.False(g.IsMember(u));
  }

  [Fact, Trait("Scenario", "D-GROUP-07")]
  public void Start_and_close()
  {
    var g = Make.Group(); g.Start(g.LeaderId, T0).ThrowIfFailed(); Assert.Equal(GroupStatus.Riding, g.Status);
    g.Close(g.LeaderId, T0).ThrowIfFailed(); Assert.True(g.Close(g.LeaderId, T0).IsSuccess);
    g.Start(g.LeaderId, T0).ShouldFailWith("GROUP_CLOSED");
  }

  [Fact, Trait("Scenario", "D-GROUP-08")]
  public void Regenerate_code_on_closed() { var g = Make.Group(); g.Close(g.LeaderId, T0); g.RegenerateCode(g.LeaderId, "ZZZZZZ", T0).ShouldFailWith("GROUP_CLOSED"); }

  [Fact, Trait("Scenario", "D-GROUP-09")]
  public void Meeting_point_validation()
  {
    var g = Make.Group();
    g.SetMeetingPoint(g.LeaderId, "x", 99, 0, null, T0).ShouldFailWith("INVALID_LOCATION");
    g.SetMeetingPoint(g.LeaderId, "x", 10, 106, T0.AddMinutes(-1), T0).ShouldFailWith("INVALID_DEPART_TIME");
    g.SetMeetingPoint(g.LeaderId, "Bến xe", 10, 106, T0.AddHours(1), T0).ThrowIfFailed(); Assert.Equal("Bến xe", g.MeetingName);
  }

  [Fact, Trait("Scenario", "D-GROUP-10")]
  public void Removal_raises_event_and_touch_bumps_updated_at()
  {
    var g = Make.Group(); var u = Guid.NewGuid(); g.Join(u, T0, 10); g.ClearDomainEvents();
    g.Leave(u, T0.AddMinutes(5));
    Assert.Equal(u, Assert.Single(g.DomainEvents.OfType<MemberRemovedFromGroup>()).UserId);
    g.Touch(T0.AddMinutes(9)); Assert.Equal(T0.AddMinutes(9), g.UpdatedAt);
  }

  [Fact, Trait("Scenario", "D-GROUP-11")]
  public void Member_location_and_straggling()
  {
    var g = Make.Group(); var u = Guid.NewGuid(); g.Join(u, T0, 10);
    g.ReportMemberLocation(u, Make.Point(1, 2), 55, T0).ThrowIfFailed(); Assert.Equal(1, g.MemberOf(u)!.LastLat);
    var since = g.UpdateStraggling(u, true, T0); Assert.Equal(T0, since);
    Assert.Equal(T0, g.UpdateStraggling(u, true, T0.AddMinutes(1)));
    Assert.Null(g.UpdateStraggling(u, false, T0.AddMinutes(2)));
  }
}

public class GroupSupportTests
{
  [Fact, Trait("Scenario", "D-CODE-01")]
  public void Generated_codes()
  {
    for (var i = 0; i < 1000; i++) { var c = GroupCode.Generate(); Assert.Equal(6, c.Length); Assert.True(c.All(ch => "ABCDEFGHJKLMNPQRSTUVWXYZ23456789".Contains(ch))); }
  }

  [Fact, Trait("Scenario", "D-CODE-02")]
  public void Normalize() { Assert.Equal("ABC234", GroupCode.Normalize("  abc234 ")); Assert.Null(GroupCode.Normalize("ABC10I")); Assert.Null(GroupCode.Normalize("ABC")); }

  [Fact, Trait("Scenario", "D-QUICK-01")]
  public void Quick_messages()
  {
    foreach (var t in new[] { "rest", "out_of_fuel", "need_help", "police" }) Assert.True(QuickMessages.IsValid(t));
    Assert.False(QuickMessages.IsValid("hello")); Assert.True(QuickMessages.PushesToGroup("need_help")); Assert.False(QuickMessages.PushesToGroup("rest"));
    Assert.Equal("Cần hỗ trợ", QuickMessages.Label("need_help"));
  }
}

public class MemoryTests
{
  private static MemoryDraft D(MemoryType t = MemoryType.Note, string? media = null, string? text = "hi", double? lat = null, double? lng = null, int? rating = null, DateTimeOffset? at = null) =>
    new(t, media, text, lat, lng, null, null, rating, at);

  [Fact, Trait("Scenario", "D-MEM-01")]
  public void Required_parts()
  {
    Memory.Create(Guid.NewGuid(), D(text: " "), Make.Now).ShouldFailWith("TEXT_REQUIRED");
    Memory.Create(Guid.NewGuid(), D(MemoryType.Photo, media: null), Make.Now).ShouldFailWith("MEDIA_REQUIRED");
  }

  [Theory, Trait("Scenario", "D-MEM-02")]
  [InlineData(0, false), InlineData(1, true), InlineData(5, true), InlineData(6, false)]
  public void Rating(int r, bool ok)
  {
    var res = Memory.Create(Guid.NewGuid(), D(rating: r), Make.Now);
    if (ok) Assert.True(res.IsSuccess); else res.ShouldFailWith("INVALID_RATING");
  }

  [Fact, Trait("Scenario", "D-MEM-03")]
  public void Lat_without_lng() => Memory.Create(Guid.NewGuid(), D(lat: 10), Make.Now).ShouldFailWith("INVALID_LOCATION");

  [Fact, Trait("Scenario", "D-MEM-04")]
  public void Defaults()
  {
    var m = Memory.Create(Guid.NewGuid(), D(media: "users/x/y.jpg"), Make.Now).Value;
    Assert.Equal(Make.Now, m.TakenAt); Assert.Null(m.MediaPath);
  }
}
