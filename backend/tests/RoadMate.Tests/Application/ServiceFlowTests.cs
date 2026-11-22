using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using RoadMate.Application.Accounts;
using RoadMate.Application.Accounts.DTOs.Requests;
using RoadMate.Application.Accounts.DTOs.Responses;
using RoadMate.Application.Accounts.Interfaces;
using RoadMate.Application.Contacts;
using RoadMate.Application.Contacts.Interfaces;
using RoadMate.Application.Groups;
using RoadMate.Application.Groups.Interfaces;
using RoadMate.Application.Shared;
using RoadMate.Application.Trips;
using RoadMate.Application.Trips.DTOs.Requests;
using RoadMate.Application.Trips.DTOs.Responses;
using RoadMate.Application.Trips.Interfaces;
using RoadMate.Tests.TestKit;

namespace RoadMate.Tests.Application;

public class AccountAndContactTests
{
  [Fact, Trait("Scenario", "EXTRA-A-AUTH-01")]
  public async Task First_sync_creates_user_with_name_fallbacks()
  {
    var w = new World(); var svc = w.Get<IAuthService>();
    var r = await svc.SyncAsync(new TokenIdentity("fb-1", "an.nguyen@x.vn", null, null), new SyncRequest("fcm-1", null), default);
    Assert.True(r.IsSuccess); Assert.Equal("an.nguyen", r.Value.Name); Assert.Single(w.Users); Assert.Equal("fcm-1", w.Users[0].FcmToken);
    var again = await svc.SyncAsync(new TokenIdentity("fb-1", "an.nguyen@x.vn", "Khác", null), new SyncRequest(null, null), default);
    Assert.Single(w.Users); Assert.Equal(r.Value.Id, again.Value.Id);
  }

  [Fact, Trait("Scenario", "EXTRA-A-AUTH-02")]
  public async Task Sync_with_verified_phone_links_existing_contacts_of_other_users()
  {
    var w = new World(); var owner = w.AddUser(); var contact = w.AddContact(owner, "0912345678");
    var r = await w.Get<IAuthService>().SyncAsync(new TokenIdentity("fb-mom", null, "Mẹ", "+84912345678"), new SyncRequest("tok", null), default);
    Assert.Equal(r.Value.Id, contact.LinkedUserId);
  }

  [Fact, Trait("Scenario", "EXTRA-A-AUTH-03")]
  public async Task Sync_race_returns_the_existing_user()
  {
    var w = new World(); w.ThrowUniqueOnNextSave = true;
    var existing = w.AddUser(); // simulate: the winner of the race already exists
    var r = await w.Get<IAuthService>().SyncAsync(new TokenIdentity(existing.FirebaseUid, null, "x", null), new SyncRequest(null, null), default);
    Assert.True(r.IsSuccess); Assert.Equal(existing.Id, r.Value.Id);
  }

  [Fact, Trait("Scenario", "A-CONTACT-01")]
  public async Task Create_normalizes_phone_links_account_and_enforces_limit()
  {
    var w = new World(); var owner = w.AddUser(); var mom = w.AddUser("Mẹ", phone: "+84912345678"); var svc = w.Get<IEmergencyContactService>();
    var c = await svc.CreateAsync(owner.Id, new ContactRequest("Mẹ", "0912 345 678", "mẹ", null), default);
    Assert.Equal("+84912345678", c.Value.Phone); Assert.True(c.Value.HasApp); Assert.Equal(1, c.Value.Priority);
    for (var i = 0; i < 4; i++) await svc.CreateAsync(owner.Id, new ContactRequest("C" + i, "09111111" + (10 + i), null, null), default);
    (await svc.CreateAsync(owner.Id, new ContactRequest("C6", "0922222222", null, null), default)).ShouldFailWith("CONTACT_LIMIT");
  }

  [Fact, Trait("Scenario", "A-CONTACT-02")]
  public async Task Update_and_delete_are_owner_scoped()
  {
    var w = new World(); var owner = w.AddUser(); var other = w.AddUser("Khác"); var c = w.AddContact(owner); var svc = w.Get<IEmergencyContactService>();
    (await svc.UpdateAsync(other.Id, c.Id, new ContactRequest("X", "0912345678", null, null), default)).ShouldFailWith("CONTACT_NOT_FOUND");
    (await svc.DeleteAsync(other.Id, c.Id, default)).ShouldFailWith("CONTACT_NOT_FOUND");
    var upd = await svc.UpdateAsync(owner.Id, c.Id, new ContactRequest("Bố", "0987654321", "bố", 2), default);
    Assert.Equal("+84987654321", upd.Value.Phone); Assert.Equal(2, upd.Value.Priority);
    Assert.True((await svc.DeleteAsync(owner.Id, c.Id, default)).IsSuccess); Assert.True(c.IsDeleted);
  }

  [Fact, Trait("Scenario", "A-CONTACT-03")]
  public async Task Test_message_goes_to_sms_and_to_push_when_linked()
  {
    var w = new World(); var owner = w.AddUser(); var mom = w.AddUser("Mẹ", fcm: "tok"); var c = w.AddContact(owner, "0912345678", mom.Id);
    var r = await w.Get<IContactTestService>().TestAsync(owner.Id, c.Id, default);
    Assert.Equal(2, r.Value.Count); Assert.All(r.Value, d => Assert.Equal("sent", d.Status));
    Assert.Single(w.Sms.Sent); Assert.Single(w.PushCh.Sent);
  }
}

public class TripCrudTests
{
  private static TripUpsertRequest Req(string title = "Đà Lạt", List<Guid>? contacts = null, int waypoints = 1) => new(title, new LocationResponse("A", 10, 106), new LocationResponse("B", 11, 107),
    Enumerable.Range(0, waypoints).Select(i => new WaypointInput("W" + i, 10.5, 106.5, 15)).ToList(), null, 300, 480, false, false, null, null, "high", false, contacts);

  [Fact, Trait("Scenario", "EXTRA-A-TRIP-01")]
  public async Task Create_get_update_delete()
  {
    var w = new World(); var u = w.AddUser(); var c = w.AddContact(u); var svc = w.Get<ITripService>();
    var created = await svc.CreateAsync(u.Id, Req(contacts: [c.Id], waypoints: 2), default);
    Assert.True(created.IsSuccess); Assert.Equal("draft", created.Value.Status); Assert.Equal(2, created.Value.Waypoints.Count); Assert.Equal([c.Id], created.Value.AlertContactIds); Assert.Equal("high", created.Value.CrashSensitivity);
    var upd = await svc.UpdateAsync(u.Id, created.Value.Id, Req("Mới", waypoints: 0), default);
    Assert.Equal("Mới", upd.Value.Title); Assert.Empty(upd.Value.Waypoints); Assert.Empty(upd.Value.AlertContactIds);
    Assert.True((await svc.DeleteAsync(u.Id, created.Value.Id, default)).IsSuccess);
    (await svc.GetAsync(u.Id, created.Value.Id, default)).ShouldFailWith("TRIP_NOT_FOUND");
  }

  [Fact, Trait("Scenario", "EXTRA-A-TRIP-02")]
  public async Task Foreign_contact_rejected_and_active_trip_cannot_be_edited_or_deleted()
  {
    var w = new World(); var u = w.AddUser(); var other = w.AddUser("Khác"); var foreign = w.AddContact(other); var svc = w.Get<ITripService>();
    (await svc.CreateAsync(u.Id, Req(contacts: [foreign.Id]), default)).ShouldFailWith("INVALID_CONTACT");
    var active = w.AddActiveTrip(u);
    (await svc.UpdateAsync(u.Id, active.Id, Req(), default)).ShouldFailWith("TRIP_NOT_DRAFT");
    (await svc.DeleteAsync(u.Id, active.Id, default)).ShouldFailWith("TRIP_ACTIVE");
    (await svc.UpdateAsync(other.Id, active.Id, Req(), default)).ShouldFailWith("TRIP_NOT_FOUND");
  }

  [Fact, Trait("Scenario", "EXTRA-A-TRIP-03")]
  public async Task List_filters_by_status_and_unknown_status_is_empty()
  {
    var w = new World(); var u = w.AddUser(); w.AddActiveTrip(u); await w.Get<ITripService>().CreateAsync(u.Id, Req(), default);
    var svc = w.Get<ITripService>();
    Assert.Equal(2, (await svc.ListAsync(u.Id, null, default)).Value.Count);
    Assert.Single((await svc.ListAsync(u.Id, "draft", default)).Value);
    Assert.Empty((await svc.ListAsync(u.Id, "bogus", default)).Value);
  }

  [Fact, Trait("Scenario", "A-LIFE-04")]
  public async Task Rest_expected_return_and_summary_guards()
  {
    var w = new World(); var u = w.AddUser(); var t = w.AddActiveTrip(u); var svc = w.Get<ITripLifecycleService>();
    new RestRequestValidator().Validate(new RestRequest(4), w.Clock.Now).ShouldFailWith("INVALID_REST");
    Assert.NotNull((await svc.SetRestAsync(u.Id, t.Id, 30, default)).Value.RestUntil);
    Assert.Null((await svc.ClearRestAsync(u.Id, t.Id, default)).Value.RestUntil);
    (await svc.ExtendReturnAsync(u.Id, t.Id, w.Clock.Now.UtcDateTime.AddHours(-1), default)).ShouldFailWith("INVALID_RETURN_TIME");
    (await svc.SummaryAsync(u.Id, t.Id, default)).ShouldFailWith("TRIP_NOT_FINISHED");
  }
}

public class GroupFlowTests
{
  [Fact, Trait("Scenario", "A-GROUP-01")]
  public async Task Create_join_message_and_leader_handover()
  {
    var w = new World(); var leader = w.AddUser("Trưởng đoàn"); var member = w.AddUser("An"); var svc = w.Get<IGroupService>();
    var g = (await svc.CreateAsync(leader.Id, new CreateGroupRequest("Đoàn A", null, null, null, null), default)).Value;
    Assert.Equal(6, g.Code.Length);
    var joined = await svc.JoinAsync(member.Id, new JoinGroupRequest(g.Code.ToLowerInvariant()), default);
    Assert.True(joined.IsSuccess); Assert.Equal(2, joined.Value.Members.Count);
    (await svc.JoinAsync(member.Id, new JoinGroupRequest(g.Code), default)).ThrowIfFailed();           // idempotent
    (await svc.JoinAsync(member.Id, new JoinGroupRequest("ZZZZZ9"), default)).ShouldFailWith("GROUP_CODE_NOT_FOUND");

    var msg = await w.Get<IGroupMessageService>().SendMessageAsync(member.Id, g.Id, new SendGroupMessageRequest("need_help"), default);
    Assert.Equal("Cần hỗ trợ", msg.Value.Label); Assert.Single(w.Push.Calls);                         // only need_help pushes
    await w.Get<IGroupMessageService>().SendMessageAsync(member.Id, g.Id, new SendGroupMessageRequest("rest"), default);
    Assert.Single(w.Push.Calls);

    (await svc.LeaveAsync(leader.Id, g.Id, default)).ThrowIfFailed();
    Assert.Equal(member.Id, w.Groups[0].LeaderId);
    (await svc.GetAsync(leader.Id, g.Id, default)).ShouldFailWith("GROUP_NOT_FOUND");                 // non-member = not found
  }

  [Fact, Trait("Scenario", "A-GROUP-02")]
  public async Task Leaving_detaches_the_members_trips()
  {
    var w = new World(); var leader = w.AddUser(); var member = w.AddUser("An"); var svc = w.Get<IGroupService>();
    var g = (await svc.CreateAsync(leader.Id, new CreateGroupRequest("Đoàn", null, null, null, null), default)).Value;
    await svc.JoinAsync(member.Id, new JoinGroupRequest(g.Code), default);
    var trip = w.AddActiveTrip(member); (await svc.AttachTripAsync(member.Id, trip.Id, g.Id, default)).ThrowIfFailed();
    Assert.Equal(g.Id, trip.GroupId);
    await svc.LeaveAsync(member.Id, g.Id, default);
    Assert.Null(trip.GroupId);                                                                          // MemberRemovedFromGroup handler
  }

  [Fact, Trait("Scenario", "A-GROUP-03")]
  public async Task Heartbeat_position_reaches_the_group_member_via_event_handler()
  {
    var w = new World(); var leader = w.AddUser(); var svc = w.Get<IGroupService>();
    var g = (await svc.CreateAsync(leader.Id, new CreateGroupRequest("Đoàn", null, null, null, null), default)).Value;
    var trip = w.AddActiveTrip(leader); await svc.AttachTripAsync(leader.Id, trip.Id, g.Id, default);
    await w.Get<ITelemetryService>().HeartbeatAsync(leader.Id, trip.Id, new(10.5, 106.5, 20, 70, true), default);
    Assert.Single(w.GroupLocations, x => x.Group == g.Id && x.User == leader.Id);
  }
}
