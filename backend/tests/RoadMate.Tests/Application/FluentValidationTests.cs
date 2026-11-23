using System;
using System.Collections.Generic;
using System.Linq;
using FluentValidation;
using Microsoft.Extensions.Options;
using RoadMate.Application.Abstractions.Storage;
using RoadMate.Application.Shared;
using RoadMate.Tests.TestKit;

namespace RoadMate.Tests.Application;

public class FluentValidationTests
{
  [Fact]
  public void AddApplication_discovers_and_registers_all_request_validators()
  {
    var w = new World();
    Assert.IsType<UpdateProfileRequestValidator>(w.Get<IValidator<UpdateProfileRequest>>());
    Assert.IsType<CreateAlertRequestValidator>(w.Get<IValidator<CreateAlertRequest>>());
    Assert.IsType<AlertLocationRequestValidator>(w.Get<IValidator<AlertLocationRequest>>());
    Assert.IsType<SimulateAlertRequestValidator>(w.Get<IValidator<SimulateAlertRequest>>());
    Assert.IsType<ContactRequestValidator>(w.Get<IValidator<ContactRequest>>());
    Assert.IsType<CreateGroupRequestValidator>(w.Get<IValidator<CreateGroupRequest>>());
    Assert.IsType<JoinGroupRequestValidator>(w.Get<IValidator<JoinGroupRequest>>());
    Assert.IsType<MeetingPointRequestValidator>(w.Get<IValidator<MeetingPointRequest>>());
    Assert.IsType<SendGroupMessageRequestValidator>(w.Get<IValidator<SendGroupMessageRequest>>());
    Assert.IsType<MemoryRequestValidator>(w.Get<IValidator<MemoryRequest>>());
    Assert.IsType<UploadUrlRequestValidator>(w.Get<IValidator<UploadUrlRequest>>());
    Assert.IsType<DirectionsRequestValidator>(w.Get<IValidator<DirectionsRequest>>());
    Assert.IsType<DistanceMatrixRequestValidator>(w.Get<IValidator<DistanceMatrixRequest>>());
    Assert.IsType<RefreshRouteRequestValidator>(w.Get<IValidator<RefreshRouteRequest>>());
    Assert.IsType<AddPointsRequestValidator>(w.Get<IValidator<AddPointsRequest>>());
    Assert.IsType<HeartbeatRequestValidator>(w.Get<IValidator<HeartbeatRequest>>());
    Assert.IsType<ExtendReturnRequestValidator>(w.Get<IValidator<ExtendReturnRequest>>());
    Assert.IsType<RestRequestValidator>(w.Get<IValidator<RestRequest>>());
    Assert.IsType<StartTripRequestValidator>(w.Get<IValidator<StartTripRequest>>());
    Assert.IsType<TripUpsertRequestValidator>(w.Get<IValidator<TripUpsertRequest>>());
  }

  [Fact]
  public void UpdateProfileRequestValidator_checks_safety_settings_and_boundaries()
  {
    var v = new UpdateProfileRequestValidator();
    UpdateProfileRequest R(string? sens = null, int? idle = null) =>
      new(null, null, null, null, null, null, idle, sens, null, null, null);

    Assert.True(v.Validate(R()).IsValid);
    Assert.True(v.Validate(R("low", 1)).IsValid);
    Assert.True(v.Validate(R("HIGH", 240)).IsValid);

    v.Validate(R("extreme", 10)).ShouldFailWith("INVALID_SENSITIVITY", nameof(UpdateProfileRequest.CrashSensitivity));
    v.Validate(R("medium", 0)).ShouldFailWith("INVALID_IDLE", nameof(UpdateProfileRequest.IdleMinutes));
    v.Validate(R("medium", 241)).ShouldFailWith("INVALID_IDLE", nameof(UpdateProfileRequest.IdleMinutes));
  }

  [Fact]
  public void CreateAlertRequestValidator_checks_required_boundaries_and_precedence()
  {
    var v = new CreateAlertRequestValidator();
    Assert.True(v.Validate(new CreateAlertRequest("c-1", "crash", -90, 180, 50)).IsValid);
    Assert.True(v.Validate(new CreateAlertRequest(new string('a', 100), "sos", 90, -180, null)).IsValid);

    v.Validate(new CreateAlertRequest("   ", "crash", 10, 106, null)).ShouldFailWith("INVALID_CLIENT_ID", nameof(CreateAlertRequest.ClientId));
    v.Validate(new CreateAlertRequest(new string('a', 101), "crash", 10, 106, null)).ShouldFailWith("INVALID_CLIENT_ID", nameof(CreateAlertRequest.ClientId));
    v.Validate(new CreateAlertRequest("c-1", "invalid", 10, 106, null)).ShouldFailWith("INVALID_TYPE", nameof(CreateAlertRequest.Type));
    v.Validate(new CreateAlertRequest("c-1", "crash", double.NaN, 106, null)).ShouldFailWith("INVALID_LOCATION", "Location");
    v.Validate(new CreateAlertRequest("c-1", "crash", 10, 180.1, null)).ShouldFailWith("INVALID_LOCATION", "Location");
  }

  [Fact]
  public void AlertLocationRequestValidator_checks_coordinates()
  {
    var v = new AlertLocationRequestValidator();
    Assert.True(v.Validate(new AlertLocationRequest(10.7, 106.7, 80)).IsValid);
    Assert.True(v.Validate(new AlertLocationRequest(-90, -180, null)).IsValid);
    v.Validate(new AlertLocationRequest(90.1, 0, null)).ShouldFailWith("INVALID_LOCATION", "Location");
    v.Validate(new AlertLocationRequest(0, double.PositiveInfinity, null)).ShouldFailWith("INVALID_LOCATION", "Location");
  }

  [Fact]
  public void SimulateAlertRequestValidator_validates_when_enabled_and_skips_when_disabled()
  {
    var on = new SimulateAlertRequestValidator(Options.Create(new DebugOptions { EnableSimulator = true }));
    Assert.True(on.Validate(new SimulateAlertRequest(Guid.NewGuid(), null, null, null, null)).IsValid);
    Assert.True(on.Validate(new SimulateAlertRequest(Guid.NewGuid(), "sos", 10.5, 106.5, true)).IsValid);
    on.Validate(new SimulateAlertRequest(Guid.NewGuid(), "unknown", 10.5, 106.5, true)).ShouldFailWith("INVALID_TYPE", nameof(SimulateAlertRequest.Type));
    on.Validate(new SimulateAlertRequest(Guid.NewGuid(), "crash", 95, 106.5, true)).ShouldFailWith("INVALID_LOCATION", "Location");

    var off = new SimulateAlertRequestValidator(Options.Create(new DebugOptions { EnableSimulator = false }));
    Assert.True(off.Validate(new SimulateAlertRequest(Guid.NewGuid(), "unknown", 999, 999, true)).IsValid);
  }

  [Fact]
  public void ContactRequestValidator_checks_name_and_vietnam_phone()
  {
    var v = new ContactRequestValidator();
    Assert.True(v.Validate(new ContactRequest("Mẹ", "0912345678", "mẹ", 1)).IsValid);
    Assert.True(v.Validate(new ContactRequest("Bố", "84912345678", null, null)).IsValid);
    v.Validate(new ContactRequest("", "0912345678", null, null)).ShouldFailWith("INVALID_NAME", nameof(ContactRequest.Name));
    v.Validate(new ContactRequest("Mẹ", "+1234567890", null, null)).ShouldFailWith("INVALID_PHONE", nameof(ContactRequest.Phone));
  }

  [Fact]
  public void CreateGroupRequestValidator_checks_name_meeting_pair_and_depart_time()
  {
    var clock = new TestClock();
    var v = new CreateGroupRequestValidator(clock);
    var future = clock.Now.UtcDateTime.AddHours(1);

    Assert.True(v.Validate(new CreateGroupRequest("Đoàn Đà Lạt", null, null, null, null)).IsValid);
    Assert.True(v.Validate(new CreateGroupRequest("  " + new string('a', 100) + "  ", "Bến xe", 10.7, 106.7, future)).IsValid);

    v.Validate(new CreateGroupRequest("  ", null, null, null, null)).ShouldFailWith("INVALID_NAME", nameof(CreateGroupRequest.Name));
    v.Validate(new CreateGroupRequest(new string('a', 101), null, null, null, null)).ShouldFailWith("INVALID_NAME", nameof(CreateGroupRequest.Name));
    v.Validate(new CreateGroupRequest("Đoàn", "X", 10.7, null, null)).ShouldFailWith("INVALID_LOCATION", "MeetingLocation");
    v.Validate(new CreateGroupRequest("Đoàn", "X", null, 106.7, null)).ShouldFailWith("INVALID_LOCATION", "MeetingLocation");
    v.Validate(new CreateGroupRequest("Đoàn", "X", 91, 106.7, null)).ShouldFailWith("INVALID_LOCATION", "MeetingLocation");
    v.Validate(new CreateGroupRequest("Đoàn", "X", 10.7, 106.7, clock.Now.UtcDateTime)).ShouldFailWith("INVALID_DEPART_TIME", nameof(CreateGroupRequest.DepartAt));
    v.Validate(new CreateGroupRequest("Đoàn", "X", 10.7, 106.7, clock.Now.UtcDateTime.AddMinutes(-1))).ShouldFailWith("INVALID_DEPART_TIME", nameof(CreateGroupRequest.DepartAt));
  }

  [Fact]
  public void JoinGroupRequestValidator_checks_group_code()
  {
    var v = new JoinGroupRequestValidator();
    Assert.True(v.Validate(new JoinGroupRequest("  abc234 ")).IsValid);
    v.Validate(new JoinGroupRequest("ABC")).ShouldFailWith("INVALID_GROUP_CODE", nameof(JoinGroupRequest.Code));
    v.Validate(new JoinGroupRequest("ABC01I")).ShouldFailWith("INVALID_GROUP_CODE", nameof(JoinGroupRequest.Code));
  }

  [Fact]
  public void MeetingPointRequestValidator_checks_location_and_future_depart_time()
  {
    var clock = new TestClock();
    var v = new MeetingPointRequestValidator(clock);
    Assert.True(v.Validate(new MeetingPointRequest("Bến xe", 10.7, 106.7, null)).IsValid);
    Assert.True(v.Validate(new MeetingPointRequest("Bến xe", 10.7, 106.7, clock.Now.UtcDateTime.AddMinutes(30))).IsValid);

    v.Validate(new MeetingPointRequest("X", -91, 106.7, null)).ShouldFailWith("INVALID_LOCATION", "Location");
    v.Validate(new MeetingPointRequest("X", 10.7, 106.7, clock.Now.UtcDateTime)).ShouldFailWith("INVALID_DEPART_TIME", nameof(MeetingPointRequest.DepartAt));
  }

  [Fact]
  public void SendGroupMessageRequestValidator_checks_quick_type()
  {
    var v = new SendGroupMessageRequestValidator();
    foreach (var t in new[] { "rest", "out_of_fuel", "need_help", "police" })
      Assert.True(v.Validate(new SendGroupMessageRequest(t)).IsValid);
    v.Validate(new SendGroupMessageRequest("hello")).ShouldFailWith("INVALID_QUICK_TYPE", nameof(SendGroupMessageRequest.QuickType));
  }

  [Fact]
  public void MemoryRequestValidator_checks_memory_type()
  {
    var v = new MemoryRequestValidator();
    foreach (var t in new[] { "photo", "note", "video", "PHOTO" })
      Assert.True(v.Validate(new MemoryRequest(t, null, null, null, null, null, null, null, null)).IsValid);
    v.Validate(new MemoryRequest("audio", null, null, null, null, null, null, null, null)).ShouldFailWith("INVALID_TYPE", nameof(MemoryRequest.Type));
  }

  [Fact]
  public void UploadUrlRequestValidator_checks_kind_content_type_and_size_boundaries()
  {
    var opts = new StorageOptions { MaxImageBytes = 5_000, MaxVideoBytes = 25_000 };
    var v = new UploadUrlRequestValidator(Options.Create(opts));

    Assert.True(v.Validate(new UploadUrlRequest("photo", "image/jpeg", 1)).IsValid);
    Assert.True(v.Validate(new UploadUrlRequest("PHOTO", "IMAGE/WEBP", 5_000)).IsValid);
    Assert.True(v.Validate(new UploadUrlRequest("video", "video/mp4", 25_000)).IsValid);

    var badKind = v.Validate(new UploadUrlRequest("audio", "audio/mp3", 999_999));
    badKind.ShouldFailWith("INVALID_KIND", nameof(UploadUrlRequest.Kind));
    Assert.Single(badKind.Errors); // conditional When prevents spurious content-type/size errors

    v.Validate(new UploadUrlRequest("photo", "video/mp4", 100)).ShouldFailWith("UNSUPPORTED_CONTENT_TYPE", nameof(UploadUrlRequest.ContentType));
    v.Validate(new UploadUrlRequest("photo", "image/jpeg", 0)).ShouldFailWith("FILE_TOO_LARGE", nameof(UploadUrlRequest.SizeBytes));
    v.Validate(new UploadUrlRequest("photo", "image/jpeg", 5_001)).ShouldFailWith("FILE_TOO_LARGE", nameof(UploadUrlRequest.SizeBytes));
    v.Validate(new UploadUrlRequest("video", "video/mp4", 25_001)).ShouldFailWith("FILE_TOO_LARGE", nameof(UploadUrlRequest.SizeBytes));
  }

  [Fact]
  public void DirectionsRequestValidator_checks_points_waypoints_and_vehicle()
  {
    var v = new DirectionsRequestValidator();
    var ok = new LatLng(10.7, 106.7);
    var dest = new LatLng(11.9, 108.4);

    Assert.True(v.Validate(new DirectionsRequest(ok, dest, null, false, false, null)).IsValid);
    Assert.True(v.Validate(new DirectionsRequest(ok, dest, Enumerable.Repeat(ok, 20).ToList(), true, true, "car")).IsValid);

    v.Validate(new DirectionsRequest(null!, dest, null, false, false, null)).ShouldFailWith("MISSING_POINTS", "Points");
    v.Validate(new DirectionsRequest(ok, null!, null, false, false, null)).ShouldFailWith("MISSING_POINTS", "Points");
    v.Validate(new DirectionsRequest(ok, dest, Enumerable.Repeat(ok, 21).ToList(), false, false, null)).ShouldFailWith("TOO_MANY_WAYPOINTS", nameof(DirectionsRequest.Waypoints));
    v.Validate(new DirectionsRequest(new LatLng(91, 0), dest, null, false, false, null)).ShouldFailWith("INVALID_LOCATION", nameof(DirectionsRequest.Origin));
    v.Validate(new DirectionsRequest(ok, dest, [new LatLng(0, 181)], false, false, null)).ShouldFailWith("INVALID_LOCATION");
    v.Validate(new DirectionsRequest(ok, new LatLng(-91, 0), null, false, false, null)).ShouldFailWith("INVALID_LOCATION", nameof(DirectionsRequest.Destination));
    v.Validate(new DirectionsRequest(ok, dest, null, false, false, "rocket")).ShouldFailWith("INVALID_VEHICLE", nameof(DirectionsRequest.Vehicle));
  }

  [Fact]
  public void DistanceMatrixRequestValidator_checks_points_limits_and_vehicle()
  {
    var v = new DistanceMatrixRequestValidator();
    var ok = new LatLng(10.7, 106.7);

    Assert.True(v.Validate(new DistanceMatrixRequest([ok], [ok], "motorbike")).IsValid);
    Assert.True(v.Validate(new DistanceMatrixRequest(Enumerable.Repeat(ok, 10).ToList(), Enumerable.Repeat(ok, 25).ToList(), null)).IsValid);

    v.Validate(new DistanceMatrixRequest([], [ok], null)).ShouldFailWith("MISSING_POINTS", "Points");
    v.Validate(new DistanceMatrixRequest([ok], [], null)).ShouldFailWith("MISSING_POINTS", "Points");
    v.Validate(new DistanceMatrixRequest(Enumerable.Repeat(ok, 11).ToList(), [ok], null)).ShouldFailWith("TOO_MANY_POINTS", "Points");
    v.Validate(new DistanceMatrixRequest([ok], Enumerable.Repeat(ok, 26).ToList(), null)).ShouldFailWith("TOO_MANY_POINTS", "Points");
    v.Validate(new DistanceMatrixRequest([new LatLng(99, 0)], [ok], null)).ShouldFailWith("INVALID_LOCATION");
    v.Validate(new DistanceMatrixRequest([ok], [new LatLng(0, -181)], null)).ShouldFailWith("INVALID_LOCATION");
    v.Validate(new DistanceMatrixRequest([ok], [ok], "plane")).ShouldFailWith("INVALID_VEHICLE", nameof(DistanceMatrixRequest.Vehicle));
  }

  [Fact]
  public void RefreshRouteRequestValidator_checks_optional_from_and_vehicle()
  {
    var v = new RefreshRouteRequestValidator();
    Assert.True(v.Validate(new RefreshRouteRequest(null, null, null)).IsValid);
    Assert.True(v.Validate(new RefreshRouteRequest(new LatLng(10.7, 106.7), 1, "car")).IsValid);

    v.Validate(new RefreshRouteRequest(new LatLng(95, 106.7), 0, null)).ShouldFailWith("INVALID_LOCATION", nameof(RefreshRouteRequest.From));
    v.Validate(new RefreshRouteRequest(null, 0, "bus")).ShouldFailWith("INVALID_VEHICLE", nameof(RefreshRouteRequest.Vehicle));
  }

  [Fact]
  public void AddPointsRequestValidator_checks_null_batch_size_and_point_coordinates()
  {
    var v = new AddPointsRequestValidator();
    var p = new PointRequest(10.7, 106.7, 20, 5, 80, Make.Now.UtcDateTime);

    Assert.True(v.Validate(new AddPointsRequest([])).IsValid);
    Assert.True(v.Validate(new AddPointsRequest(Enumerable.Repeat(p, 500).ToList())).IsValid);

    v.Validate(new AddPointsRequest(null!)).ShouldFailWith("INVALID_POINTS", nameof(AddPointsRequest.Points));
    v.Validate(new AddPointsRequest(Enumerable.Repeat(p, 501).ToList())).ShouldFailWith("BATCH_TOO_LARGE", nameof(AddPointsRequest.Points));
    v.Validate(new AddPointsRequest([p with { Lng = 181 }])).ShouldFailWith("INVALID_LOCATION");
  }

  [Fact]
  public void HeartbeatRequestValidator_checks_coordinates()
  {
    var v = new HeartbeatRequestValidator();
    Assert.True(v.Validate(new HeartbeatRequest(10.7, 106.7, 30, 80, true)).IsValid);
    v.Validate(new HeartbeatRequest(-90.1, 106.7, 30, 80, true)).ShouldFailWith("INVALID_LOCATION", "Location");
  }

  [Fact]
  public void Trip_timing_and_rest_validators_check_boundaries()
  {
    var clock = new TestClock();

    var rest = new RestRequestValidator();
    Assert.True(rest.Validate(new RestRequest(5)).IsValid);
    Assert.True(rest.Validate(new RestRequest(480)).IsValid);
    rest.Validate(new RestRequest(4)).ShouldFailWith("INVALID_REST", nameof(RestRequest.Minutes));
    rest.Validate(new RestRequest(481)).ShouldFailWith("INVALID_REST", nameof(RestRequest.Minutes));

    var ext = new ExtendReturnRequestValidator(clock);
    Assert.True(ext.Validate(new ExtendReturnRequest(clock.Now.UtcDateTime.AddMinutes(1))).IsValid);
    ext.Validate(new ExtendReturnRequest(clock.Now.UtcDateTime)).ShouldFailWith("INVALID_RETURN_TIME", nameof(ExtendReturnRequest.ExpectedReturnTime));
    ext.Validate(new ExtendReturnRequest(clock.Now.UtcDateTime.AddMinutes(-5))).ShouldFailWith("INVALID_RETURN_TIME", nameof(ExtendReturnRequest.ExpectedReturnTime));

    var start = new StartTripRequestValidator(clock);
    Assert.True(start.Validate(new StartTripRequest(null, null, null)).IsValid);
    Assert.True(start.Validate(new StartTripRequest(clock.Now.UtcDateTime.AddHours(2), null, true)).IsValid);
    start.Validate(new StartTripRequest(clock.Now.UtcDateTime, null, null)).ShouldFailWith("INVALID_RETURN_TIME", nameof(StartTripRequest.ExpectedReturnTime));
  }

  [Fact]
  public void TripUpsertRequestValidator_checks_null_locations_and_waypoint_coordinates()
  {
    var v = new TripUpsertRequestValidator();
    var ok = new LocationResponse("A", 10, 106);

    v.Validate(new TripUpsertRequest("T", null!, ok, null, null, null, null, false, false, null, null, null, false, null))
      .ShouldFailWith("INVALID_LOCATION", nameof(TripUpsertRequest.Origin));
    v.Validate(new TripUpsertRequest("T", ok, null!, null, null, null, null, false, false, null, null, null, false, null))
      .ShouldFailWith("INVALID_LOCATION", nameof(TripUpsertRequest.Destination));
    v.Validate(new TripUpsertRequest("T", ok, ok, [new WaypointRequest("W", 95, 106, null)], null, null, null, false, false, null, null, null, false, null))
      .ShouldFailWith("INVALID_LOCATION");
  }
}
