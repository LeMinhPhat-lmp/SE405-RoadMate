namespace RoadMate.Infrastructure.Maps;

/// <summary>
/// Cấu hình Goong REST API V2 (section "Goong"). Khoá bí mật nạp qua biến môi trường <c>Goong__ApiKey</c>
/// (hoặc <c>GOONG_API_KEY</c> trong file .env) / user-secrets, KHÔNG ghi vào appsettings.json.
/// </summary>
public class GoongOptions
{
  /// <summary>REST API key của Goong (khác Maptiles key dùng hiển thị bản đồ trên Flutter).</summary>
  public string ApiKey { get; set; } = "";
  public string BaseUrl { get; set; } = "https://rsapi.goong.io";
  /// <summary>Timeout mỗi request tới Goong.</summary>
  public int TimeoutSeconds { get; set; } = 10;

  /// <summary>true: gửi has_deprecated_administrative_unit=true để nhận thêm địa chỉ theo địa giới CŨ (trước sáp nhập).</summary>
  public bool IncludeDeprecatedAdministrativeUnit { get; set; }

  /// <summary>Giá trị vehicle gửi Goong cho xe máy. Doc Directions V2 liệt kê "bike", Distance Matrix liệt kê thêm "motorcycle" → chỉnh ở đây nếu cần.</summary>
  public string MotorbikeVehicle { get; set; } = "bike";
  public string CarVehicle { get; set; } = "car";

  /// <summary>Số request song song tối đa khi tính nhiều chặng/lấy chi tiết nhiều địa điểm.</summary>
  public int MaxParallelRequests { get; set; } = 4;

  public int AutocompleteCacheSeconds { get; set; } = 120;
  public int DetailCacheMinutes { get; set; } = 60;
  public int ReverseCacheMinutes { get; set; } = 10;
  public int NearbyCacheMinutes { get; set; } = 5;
}
