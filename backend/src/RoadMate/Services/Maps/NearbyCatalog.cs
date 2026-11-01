namespace RoadMate.Services.Maps;

/// <summary>Loại tiện ích dọc đường → từ khoá tìm kiếm tiếng Việt (Goong không có API tìm theo loại POI trong bộ doc V2 hiện có).</summary>
public static class NearbyCatalog
{
  private static readonly Dictionary<string, string> Keywords = new(StringComparer.OrdinalIgnoreCase)
  {
    ["gas_station"] = "trạm xăng",
    ["repair"] = "sửa xe máy",
    ["hospital"] = "bệnh viện",
    ["clinic"] = "trạm y tế",
    ["pharmacy"] = "nhà thuốc",
    ["atm"] = "ATM",
    ["lodging"] = "nhà nghỉ",
    ["homestay"] = "homestay",
    ["food"] = "quán ăn",
    ["cafe"] = "quán cà phê"
  };

  public static IReadOnlyCollection<string> Types => Keywords.Keys;
  public static bool TryGetKeyword(string type, out string keyword) => Keywords.TryGetValue(type ?? "", out keyword!);
}
