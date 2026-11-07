using RoadMate.Infrastructure.Weather;

namespace RoadMate.Services.Weather;

public static class WeatherAssessor
{
  public const string HeavyRain = "HEAVY_RAIN", StrongWind = "STRONG_WIND", Thunderstorm = "THUNDERSTORM";

  /// <summary>
  /// Cảnh báo thời tiết xấu trong <see cref="WeatherOptions.LookaheadHours"/> giờ tới (gồm cả hiện tại): mưa to, gió giật mạnh, giông.
  /// Mỗi loại chỉ báo một lần, ở thời điểm sớm nhất.
  /// </summary>
  public static List<WeatherWarningDto> Assess(WeatherForecast f, WeatherOptions o, DateTime now)
  {
    var warnings = new Dictionary<string, WeatherWarningDto>();
    void Add(string code, string message, DateTime at) => warnings.TryAdd(code, new WeatherWarningDto(code, message, at));

    Check(f.Current.Time, f.Current.PrecipitationMm, f.Current.GustKmh, f.Current.WeatherCode);
    var from = new DateTime(now.Year, now.Month, now.Day, now.Hour, 0, 0, DateTimeKind.Utc);
    foreach (var h in f.Hourly.Where(h => h.Time >= from && h.Time <= now.AddHours(o.LookaheadHours)))
      Check(h.Time, h.PrecipitationMm, h.GustKmh, h.WeatherCode);
    return warnings.Values.OrderBy(w => w.At).ToList();

    void Check(DateTime at, double rainMm, double gustKmh, int code)
    {
      if (rainMm >= o.HeavyRainMmPerHour) Add(HeavyRain, $"Mưa to khoảng {rainMm:0.#} mm/giờ", at);
      if (gustKmh >= o.StrongGustKmh) Add(StrongWind, $"Gió giật mạnh khoảng {gustKmh:0} km/h", at);
      if (WeatherCodes.IsThunderstorm(code)) Add(Thunderstorm, $"Dự báo {WeatherCodes.Describe(code).ToLowerInvariant()}", at);
    }
  }
}
