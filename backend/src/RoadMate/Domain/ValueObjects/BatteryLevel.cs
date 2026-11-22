using RoadMate.Domain.Common;

namespace RoadMate.Domain.ValueObjects;

/// <summary>Battery percentage 0–100.</summary>
public readonly record struct BatteryLevel
{
  private BatteryLevel(int percent) => Percent = percent;
  public int Percent { get; }
  public static Result<BatteryLevel> Create(int percent) => percent is < 0 or > 100 ? Errors.InvalidBattery : new BatteryLevel(percent);
}
