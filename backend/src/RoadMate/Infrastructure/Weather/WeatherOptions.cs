namespace RoadMate.Infrastructure.Weather;

/// <summary>Cấu hình thời tiết (section "Weather"). Nguồn: Open-Meteo, không cần API key.</summary>
public class WeatherOptions
{
  public string BaseUrl { get; set; } = "https://api.open-meteo.com";
  public int TimeoutSeconds { get; set; } = 10;
  /// <summary>Thời gian cache dự báo theo ô lưới.</summary>
  public int CacheMinutes { get; set; } = 30;
  /// <summary>Làm tròn toạ độ theo ô lưới (độ) để nhiều điểm gần nhau dùng chung một lần gọi (0.1° ≈ 11 km).</summary>
  public double GridDegrees { get; set; } = 0.1;
  public int MaxParallelRequests { get; set; } = 4;

  /// <summary>Số giờ dự báo trả về trong API.</summary>
  public int ForecastHours { get; set; } = 6;
  /// <summary>Mưa to: lượng mưa mỗi giờ từ ngưỡng này (mm).</summary>
  public double HeavyRainMmPerHour { get; set; } = 8;
  /// <summary>Gió mạnh: gió giật từ ngưỡng này (km/h).</summary>
  public double StrongGustKmh { get; set; } = 50;
  /// <summary>Cảnh báo chỉ xét trong số giờ tới.</summary>
  public int LookaheadHours { get; set; } = 2;

  // ---- Job cảnh báo thời tiết xấu cho chuyến đang chạy
  public int WatchIntervalMinutes { get; set; } = 20;
  /// <summary>Tối đa số điểm phía trước được kiểm tra mỗi lần (gần nhất trước).</summary>
  public int WatchMaxPoints { get; set; } = 3;
  /// <summary>Chỉ xét điểm cách vị trí hiện tại không quá số km này.</summary>
  public double WatchLookaheadKm { get; set; } = 100;
  /// <summary>Điểm cách vị trí hiện tại dưới số mét này coi như đã tới, bỏ qua.</summary>
  public double WatchArrivedMeters { get; set; } = 1000;
  /// <summary>Không nhắc lại cùng một điểm trong khoảng này.</summary>
  public int NotifyCooldownMinutes { get; set; } = 60;
}
