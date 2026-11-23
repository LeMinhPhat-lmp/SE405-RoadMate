using RoadMate.Application.Alerts;
using RoadMate.Application.Abstractions.Storage;
using RoadMate.Application.Common;
using RoadMate.Application.Contacts;
using RoadMate.Application.Journal;
using RoadMate.Application.Maps;
using RoadMate.Application.Shared;
using RoadMate.Application.Trips;
using RoadMate.Application.Weather;
using RoadMate.Domain.Alerts;
using RoadMate.Tests.TestKit;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RoadMate.Tests.Application;

public class PolylineAndTextTests
{
  [Fact, Trait("Scenario", "EXTRA-A-POLY-01")]
  public void Decode_known_google_sample()
  {
    var pts = PolylineCodec.Decode("_p~iF~ps|U_ulLnnqC_mqNvxq`@");
    Assert.Equal(3, pts.Count);
    Assert.Equal(38.5, pts[0].Lat, 5); Assert.Equal(-120.2, pts[0].Lng, 5); Assert.Equal(43.252, pts[2].Lat, 5);
  }

  [Fact, Trait("Scenario", "EXTRA-A-POLY-02")]
  public void Roundtrip_and_empty()
  {
    var src = new List<(double, double)> { (10.77690, 106.70090), (10.80000, 106.71234), (11.94040, 108.45830) };
    var back = PolylineCodec.Decode(PolylineCodec.Encode(src));
    for (var i = 0; i < src.Count; i++) { Assert.Equal(src[i].Item1, back[i].Lat, 5); Assert.Equal(src[i].Item2, back[i].Lng, 5); }
    Assert.Empty(PolylineCodec.Decode(""));
  }

  [Fact, Trait("Scenario", "EXTRA-A-TEXT-01")]
  public void Strip_diacritics_keeps_length_semantics()
  {
    Assert.Equal("Nguyen Van An - Da Lat", TextHelper.StripDiacritics("Nguyễn Văn An - Đà Lạt"));
    Assert.Equal("DUONG", TextHelper.StripDiacritics("ĐƯỜNG"));
    Assert.Equal("abc 123", TextHelper.StripDiacritics("abc 123"));
    Assert.Equal("Nguyen", TextHelper.StripDiacritics("Nguye\u0302\u0303n"));          // decomposed input
  }

  [Fact, Trait("Scenario", "EXTRA-A-TEXT-02")]
  public void Strip_html() => Assert.Equal("Rẽ phải vào Quốc lộ 20", TextHelper.StripHtmlTags("Rẽ <b>phải</b> vào <i>Quốc lộ 20</i>"));

  [Fact, Trait("Scenario", "EXTRA-A-URL-01")]
  public void Tracking_url()
  {
    Assert.Equal("https://roadmate.app/track/abc", TrackingUrl.Build("https://roadmate.app/track", "abc"));
    Assert.Equal("https://roadmate.app/track/abc", TrackingUrl.Build("https://roadmate.app/track/", "abc"));
    Assert.Null(TrackingUrl.Build("https://x", null));
  }

  [Fact, Trait("Scenario", "EXTRA-A-COORD-01")]
  public void Coordinates_use_invariant_six_decimals() => Assert.Equal("10.776900", 10.7769.ToCoordinate());
}

public class MediaTests
{
  [Fact, Trait("Scenario", "A-MEDIA-01")]
  public void Path_is_server_generated_and_ownership_checked()
  {
    var trip = Guid.NewGuid(); var p = MediaPaths.Build("uid1", trip, ".jpg");
    Assert.True(MediaPaths.IsOwnedBy(p, "uid1", trip));
    Assert.False(MediaPaths.IsOwnedBy(p, "uid2", trip));
    Assert.False(MediaPaths.IsOwnedBy(p, "uid1", Guid.NewGuid()));
    Assert.False(MediaPaths.IsOwnedBy("users/uid1/trips/" + trip + "/../../x.jpg", "uid1", trip));
    Assert.False(MediaPaths.IsOwnedBy($"users/uid1/trips/{trip}/notahexid.jpg", "uid1", trip));
  }

  [Theory, Trait("Scenario", "A-MEDIA-02")]
  [InlineData("photo", "image/jpeg", true), InlineData("photo", "image/png", true), InlineData("photo", "image/webp", true), InlineData("photo", "video/mp4", false),
   InlineData("video", "video/mp4", true), InlineData("video", "video/quicktime", true), InlineData("video", "image/jpeg", false)]
  public void Content_types(string kind, string ct, bool ok) => Assert.Equal(ok, MediaPolicy.TryGetExtension(kind, ct, out _));

  [Fact, Trait("Scenario", "A-MEDIA-03")]
  public void Size_limits_by_kind()
  {
    var o = new StorageOptions();
    Assert.Equal(5L * 1024 * 1024, MediaPolicy.MaxBytes("photo", o)); Assert.Equal(25L * 1024 * 1024, MediaPolicy.MaxBytes("video", o));
  }

  [Fact, Trait("Scenario", "EXTRA-A-MEDIA-04")]
  public async Task Upload_url_flow_and_memory_creation_with_foreign_path_rejected()
  {
    var w = new World(); var u = w.AddUser(); var t = w.AddActiveTrip(u);
    var up = await w.Get<IMediaService>().CreateUploadUrlAsync(u.Id, t.Id, new UploadUrlRequest("photo", "image/jpeg", 1000), default);
    Assert.True(up.IsSuccess); Assert.StartsWith($"users/{u.FirebaseUid}/trips/{t.Id}/", up.Value.MediaPath);
    var uploadValidator = w.Get<FluentValidation.IValidator<UploadUrlRequest>>();
    uploadValidator.Validate(new UploadUrlRequest("photo", "image/jpeg", 6 * 1024 * 1024)).ShouldFailWith("FILE_TOO_LARGE");
    uploadValidator.Validate(new UploadUrlRequest("audio", "x", 1)).ShouldFailWith("INVALID_KIND");

    var ok = await w.Get<IMemoryService>().AddAsync(u.Id, t.Id, new MemoryRequest("photo", up.Value.MediaPath, "Đèo Bảo Lộc", null, null, null, null, 5, null), default);
    Assert.True(ok.IsSuccess); Assert.StartsWith("https://storage.test/dl/", ok.Value.MediaUrl);
    var foreign = await w.Get<IMemoryService>().AddAsync(u.Id, t.Id, new MemoryRequest("photo", "users/someone-else/trips/" + t.Id + "/" + Guid.NewGuid().ToString("N") + ".jpg", null, null, null, null, null, null, null), default);
    foreign.ShouldFailWith("INVALID_MEDIA_PATH");
  }

  [Fact, Trait("Scenario", "EXTRA-A-MEDIA-05")]
  public async Task Verify_on_create_rejects_missing_or_wrong_object()
  {
    var w = new World(); var u = w.AddUser(); var t = w.AddActiveTrip(u);
    var path = MediaPaths.Build(u.FirebaseUid, t.Id, ".jpg"); var svc = w.Get<IMemoryService>();
    w.Storage.Existing = null;
    (await svc.AddAsync(u.Id, t.Id, new MemoryRequest("photo", path, null, null, null, null, null, null, null), default)).ShouldFailWith("MEDIA_OBJECT_NOT_FOUND");
    w.Storage.Existing = new StoredObject(10_000_000, "image/jpeg");
    (await svc.AddAsync(u.Id, t.Id, new MemoryRequest("photo", path, null, null, null, null, null, null, null), default)).ShouldFailWith("FILE_TOO_LARGE");
    w.Storage.Existing = new StoredObject(10, "text/html");
    (await svc.AddAsync(u.Id, t.Id, new MemoryRequest("photo", path, null, null, null, null, null, null, null), default)).ShouldFailWith("UNSUPPORTED_CONTENT_TYPE");
  }
}

public class MessageComposerTests
{
  private static readonly AlertPolicyResolver Policies = AlertPolicyResolver.CreateDefault();

  [Fact, Trait("Scenario", "A-MSG-01")]
  public void Initial_message_has_all_parts()
  {
    var a = Make.Alert(AlertType.Crash);
    var c = AlertMessageComposer.ForAlert(a, Policies.For(AlertType.Crash), new OwnerInfo("Nguyễn Văn An", "O+", "59A1-12345"), false, Make.Now, "Quốc lộ 20", "https://r.t/ack/tok");
    Assert.Contains("[RoadMate] Nguyễn Văn An nghi gặp va chạm/té ngã và không phản hồi", c.Sms);
    Assert.Contains("https://maps.google.com/?q=10.776900,106.700900", c.Sms); Assert.Contains("(gần Quốc lộ 20)", c.Sms);
    Assert.Contains("15:00 01/07", c.Sms);                           // 08:00 UTC = 15:00 UTC+7
    Assert.Contains("Nhóm máu: O+.", c.Sms); Assert.Contains("Biển số: 59A1-12345.", c.Sms); Assert.Contains("Pin: 80%.", c.Sms);
    Assert.Contains("gọi 113/115", c.Sms); Assert.Contains("https://r.t/ack/tok", c.Sms);
    Assert.Equal("alert", c.Data["type"]); Assert.Equal("crash", c.Data["alertType"]);
  }

  [Fact, Trait("Scenario", "A-MSG-02")]
  public void Repeat_and_optional_parts()
  {
    var a = Make.Alert(AlertType.Sos);
    var c = AlertMessageComposer.ForAlert(a, Policies.For(AlertType.Sos), new OwnerInfo("An", null, null), true, Make.Now);
    Assert.Contains("CẬP NHẬT", c.Sms); Assert.DoesNotContain("Nhóm máu", c.Sms); Assert.DoesNotContain("Biển số", c.Sms); Assert.DoesNotContain("gần", c.Sms); Assert.DoesNotContain("Xác nhận:", c.Sms);
  }

  [Fact, Trait("Scenario", "A-MSG-03")]
  public void Closed_variants()
  {
    Assert.Contains("AN TOÀN", AlertMessageComposer.Closed(Make.Alert(AlertType.Crash), "An").Sms);
    var lost = AlertMessageComposer.Closed(Make.Alert(AlertType.LostSignal), "An");
    Assert.Contains("tín hiệu trở lại", lost.Sms); Assert.Equal("signal_restored", lost.Data["type"]);
  }
}

public class WeatherAndMapsPureTests
{
  private static WeatherForecast F(double rain, double gust, int code) =>
    new(new WeatherNowDto(Make.Now.UtcDateTime, 25, 0, 0, "", 5, 5), [new WeatherHourDto(Make.Now.UtcDateTime.AddHours(1), 25, rain, code, "", gust)]);

  [Theory, Trait("Scenario", "A-WX-01")]
  [InlineData(7.9, 10, 0, 0), InlineData(8.0, 10, 0, 1), InlineData(0, 49.9, 0, 0), InlineData(0, 50, 0, 1), InlineData(0, 10, 95, 1)]
  public void Thresholds(double rain, double gust, int code, int warnings) =>
    Assert.Equal(warnings, WeatherAssessor.Assess(F(rain, gust, code), new WeatherOptions(), Make.Now.UtcDateTime).Count);

  [Fact, Trait("Scenario", "A-WX-02")]
  public void Codes() { Assert.True(WeatherCodes.IsThunderstorm(95)); Assert.False(WeatherCodes.IsThunderstorm(61)); Assert.False(string.IsNullOrWhiteSpace(WeatherCodes.Describe(0))); }

  [Theory, Trait("Scenario", "EXTRA-A-VEH-01")]
  [InlineData("motorbike", "bike"), InlineData("motorcycle", "bike"), InlineData("bike", "bike"), InlineData("moto", "bike"), InlineData("car", "car"), InlineData(null, "bike")]
  public void Vehicle_mapping(string? v, string expected) => Assert.Equal(expected, VehicleMap.Resolve(v, new MapOptions()).Value);

  [Fact, Trait("Scenario", "EXTRA-A-VEH-02")]
  public void Unknown_vehicle() => VehicleMap.Resolve("rocket", new MapOptions()).ShouldFailWith("INVALID_VEHICLE");
}

public class ValidatorTests
{
  [Fact, Trait("Scenario", "EXTRA-A-VAL-01")]
  public void Create_alert_validator_order()
  {
    var v = new CreateAlertRequestValidator();
    v.Validate(new CreateAlertRequest("", "crash", 1, 1, null)).ShouldFailWith("INVALID_CLIENT_ID");
    v.Validate(new CreateAlertRequest(new string('x', 101), "crash", 1, 1, null)).ShouldFailWith("INVALID_CLIENT_ID");
    v.Validate(new CreateAlertRequest("c", "boom", 1, 1, null)).ShouldFailWith("INVALID_TYPE");
    v.Validate(new CreateAlertRequest("c", "crash", 91, 1, null)).ShouldFailWith("INVALID_LOCATION");
    Assert.True(v.Validate(new CreateAlertRequest("c", "LOWBATTERY", 1, 1, null)).IsValid);
  }

  [Fact, Trait("Scenario", "EXTRA-A-VAL-02")]
  public void Trip_validator()
  {
    var v = new TripUpsertRequestValidator(); var ok = new LocationResponse("A", 10, 106);
    TripUpsertRequest R(string title = "T", int waypoints = 0, string? sens = null, int? idle = null, LocationResponse? origin = null) => new(title, origin ?? ok, ok,
      Enumerable.Range(0, waypoints).Select(i => new WaypointInput("w", 10, 106, null)).ToList(), null, null, null, false, false, null, idle, sens, false, null);
    v.Validate(R("")).ShouldFailWith("INVALID_TITLE");
    v.Validate(R(new string('x', 201))).ShouldFailWith("INVALID_TITLE");
    v.Validate(R(origin: new LocationResponse("A", 100, 0))).ShouldFailWith("INVALID_LOCATION");
    v.Validate(R(waypoints: 21)).ShouldFailWith("TOO_MANY_WAYPOINTS");
    v.Validate(R(sens: "x")).ShouldFailWith("INVALID_SENSITIVITY");
    v.Validate(R(idle: 241)).ShouldFailWith("INVALID_IDLE");
    Assert.True(v.Validate(R(waypoints: 20, sens: "HIGH", idle: 240)).IsValid);
  }

  [Fact, Trait("Scenario", "EXTRA-A-VAL-03")]
  public void Contact_validator()
  {
    var v = new ContactRequestValidator();
    v.Validate(new ContactRequest(" ", "0912345678", null, null)).ShouldFailWith("INVALID_NAME");
    v.Validate(new ContactRequest("Mẹ", "123", null, null)).ShouldFailWith("INVALID_PHONE");
    Assert.True(v.Validate(new ContactRequest("Mẹ", "+84 912 345 678", null, null)).IsValid);
  }
}
