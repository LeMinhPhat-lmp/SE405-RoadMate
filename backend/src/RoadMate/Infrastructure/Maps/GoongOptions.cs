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
}
