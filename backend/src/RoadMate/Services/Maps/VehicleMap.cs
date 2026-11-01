using RoadMate.Infrastructure.Maps;

namespace RoadMate.Services.Maps;

/// <summary>Đổi loại xe của RoadMate sang giá trị <c>vehicle</c> của Goong (cấu hình được).</summary>
public static class VehicleMap
{
  public static Result<string> Resolve(string? vehicle, GoongOptions o)
  {
    switch ((vehicle ?? "motorbike").Trim().ToLowerInvariant())
    {
      case "" or "motorbike" or "motorcycle" or "bike" or "moto": return o.MotorbikeVehicle;
      case "car": return o.CarVehicle;
      default: return Error.Validation("INVALID_VEHICLE", "vehicle phải là motorbike hoặc car.");
    }
  }
}
