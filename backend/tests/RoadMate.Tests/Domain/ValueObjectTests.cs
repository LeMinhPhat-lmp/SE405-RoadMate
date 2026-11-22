using System;
using System.Linq;
using RoadMate.Domain.Alerts;
using RoadMate.Domain.Common;
using RoadMate.Domain.ValueObjects;
using RoadMate.Tests.TestKit;

namespace RoadMate.Tests.Domain;

public class PhoneNumberTests
{
  [Theory, Trait("Scenario", "D-PHONE-01"), Trait("Scenario", "D-PHONE-02"), Trait("Scenario", "D-PHONE-03")]
  [InlineData("0912345678", "+84912345678"), InlineData("84912345678", "+84912345678"), InlineData("+84912345678", "+84912345678"), InlineData("+84 912.345-678", "+84912345678")]
  public void Normalizes_formats(string raw, string expected) => Assert.Equal(expected, PhoneNumber.ParseVietnam(raw).Value.Value);

  [Theory, Trait("Scenario", "D-PHONE-04")]
  [InlineData("+84123456789", true), InlineData("+841234567890", true), InlineData("+8412345678", false), InlineData("+84123456789012", false)]
  public void Accepts_9_or_10_digits(string raw, bool ok) => Assert.Equal(ok, PhoneNumber.ParseVietnam(raw).IsSuccess);

  [Theory, Trait("Scenario", "D-PHONE-05")]
  [InlineData(null), InlineData(""), InlineData("   "), InlineData("abc"), InlineData("+14155550123")]
  public void Rejects_invalid(string? raw) => PhoneNumber.ParseVietnam(raw).ShouldFailWith("INVALID_PHONE");

  [Fact, Trait("Scenario", "D-PHONE-06")]
  public void Masked_hides_middle_digits()
  {
    var p = Make.Phone("0912345678");
    Assert.Equal("+84******678", p.Masked());
    Assert.DoesNotContain("12345", p.Masked());
  }

  [Fact, Trait("Scenario", "D-PHONE-07")]
  public void ToLocal() => Assert.Equal("0912345678", Make.Phone("+84912345678").ToLocal());
}

public class GeoPointTests
{
  [Theory, Trait("Scenario", "D-GEO-01")]
  [InlineData(0, 0), InlineData(90, 180), InlineData(-90, -180)]
  public void Boundaries_valid(double lat, double lng) => Assert.True(GeoPoint.Create(lat, lng).IsSuccess);

  [Theory, Trait("Scenario", "D-GEO-02")]
  [InlineData(90.0001, 0), InlineData(0, -180.0001), InlineData(double.NaN, 0), InlineData(0, double.PositiveInfinity), InlineData(double.NegativeInfinity, 0)]
  public void Invalid(double lat, double lng) => GeoPoint.Create(lat, lng).ShouldFailWith("INVALID_LOCATION");

  [Fact, Trait("Scenario", "D-GEO-03")]
  public void Haversine_hcmc_hanoi()
  {
    var a = Make.Point(10.7769, 106.7009); var b = Make.Point(21.0285, 105.8542);
    var km = a.DistanceMetersTo(b) / 1000;
    Assert.InRange(km, 1137 * 0.99, 1137 * 1.01);
    Assert.Equal(a.DistanceMetersTo(b), b.DistanceMetersTo(a), 6);
    Assert.Equal(0, a.DistanceMetersTo(a), 6);
  }
}

public class SmallValueObjectTests
{
  [Fact, Trait("Scenario", "D-CLIENT-01")]
  public void ClientRequestId_rules()
  {
    Assert.Equal("abc", ClientRequestId.Create("  abc ").Value.Value);
    ClientRequestId.Create("").ShouldFailWith("INVALID_CLIENT_ID");
    Assert.True(ClientRequestId.Create(new string('x', 100)).IsSuccess);
    ClientRequestId.Create(new string('x', 101)).ShouldFailWith("INVALID_CLIENT_ID");
  }

  [Theory, Trait("Scenario", "D-BATT-01")]
  [InlineData(-1, false), InlineData(0, true), InlineData(100, true), InlineData(101, false)]
  public void Battery(int v, bool ok) => Assert.Equal(ok, BatteryLevel.Create(v).IsSuccess);

  [Fact, Trait("Scenario", "D-TOKEN-01")]
  public void Tokens_are_32_hex_and_unique()
  {
    var all = Enumerable.Range(0, 10_000).Select(_ => TokenGenerator.NewToken()).ToList();
    Assert.Equal(10_000, all.Distinct().Count());
    Assert.True(all.All(t => t.Length == 32 && t.All(c => c is (>= '0' and <= '9') or (>= 'a' and <= 'f'))));
  }
}

public class EnumWireTests
{
  [Fact, Trait("Scenario", "D-WIRE-01")]
  public void Roundtrip_all_enums()
  {
    foreach (var v in EnumWire.Values<AlertType>()) { Assert.True(EnumWire.TryParse<AlertType>(EnumWire.ToWire(v), out var back)); Assert.Equal(v, back); }
    foreach (var v in EnumWire.Values<DeliveryStatus>()) { Assert.True(EnumWire.TryParse<DeliveryStatus>(EnumWire.ToWire(v), out var back)); Assert.Equal(v, back); }
    foreach (var v in EnumWire.Values<AlertStatus>()) { Assert.True(EnumWire.TryParse<AlertStatus>(EnumWire.ToWire(v), out var back)); Assert.Equal(v, back); }
  }

  [Fact, Trait("Scenario", "D-WIRE-02")]
  public void Legacy_casing()
  {
    Assert.Equal("offroute", EnumWire.ToWire(AlertType.OffRoute));
    Assert.Equal("lowBattery", EnumWire.ToWire(AlertType.LowBattery));
    Assert.Equal("lostSignal", EnumWire.ToWire(AlertType.LostSignal));
  }

  [Fact, Trait("Scenario", "D-WIRE-03")]
  public void Parse_is_case_insensitive_and_unknown_fails_softly()
  {
    Assert.True(EnumWire.TryParse<AlertType>("CRASH", out var a)); Assert.Equal(AlertType.Crash, a);
    Assert.True(EnumWire.TryParse<AlertType>("crash", out _));
    Assert.False(EnumWire.TryParse<AlertType>("bogus", out _));
    Assert.False(EnumWire.TryParse<AlertType>(null, out _));
  }
}

public class ResultTests
{
  private static readonly Error E = Error.Validation("X", "x");

  [Fact, Trait("Scenario", "D-RESULT-01")]
  public void Railway_helpers()
  {
    Result<int> ok = 2; Result<int> bad = E;
    Assert.Equal(4, ok.Map(x => x * 2).Value);
    Assert.Equal(E, bad.Map(x => x * 2).Error);
    Assert.Equal(7, ok.Bind(x => (Result<int>)(x + 5)).Value);
    Assert.Equal(E, bad.Bind(x => (Result<int>)(x + 5)).Error);
    Assert.True(ok.Ensure(x => x > 1, E).IsSuccess);
    Assert.Equal("X", ok.Ensure(x => x > 5, E).Error.Code);
    var tapped = 0; ok.Tap(_ => tapped++); bad.Tap(_ => tapped++);
    Assert.Equal(1, tapped);
    Assert.Equal("ok", ok.Match(_ => "ok", _ => "fail"));
    Assert.Equal("fail", bad.Match(_ => "ok", _ => "fail"));
  }

  [Fact, Trait("Scenario", "D-RESULT-02")]
  public void Value_on_failure_throws() { Result<int> bad = E; Assert.Throws<InvalidOperationException>(() => _ = bad.Value); }

  [Fact, Trait("Scenario", "D-RESULT-03")]
  public void Implicit_conversions()
  {
    Result<string> a = "v"; Result b = E; Result<string> c = E;
    Assert.True(a.IsSuccess); Assert.True(b.IsFailure); Assert.True(c.IsFailure);
  }
}
