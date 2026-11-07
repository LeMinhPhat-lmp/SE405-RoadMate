using System.Text.Json;

namespace RoadMate.Services.Weather;

/// <summary>Phân tích JSON Open-Meteo (hàm thuần, thiếu field thì dùng 0 thay vì ném exception).</summary>
public static class WeatherParser
{
  public static Result<WeatherForecast> Parse(JsonElement root, DateTime fallbackNow)
  {
    if (root.GetOrNull("current") is not { } c)
      return Error.External("WEATHER_PROVIDER_ERROR", "Dịch vụ thời tiết không trả dữ liệu hiện tại.");

    var code = (int)(c.GetDoubleOrNull("weather_code") ?? 0);
    var current = new WeatherNowDto(
      c.GetDoubleOrNull("time") is { } t ? FromUnix(t) : fallbackNow,
      c.GetDoubleOrNull("temperature_2m") ?? 0, c.GetDoubleOrNull("precipitation") ?? 0, code, WeatherCodes.Describe(code),
      c.GetDoubleOrNull("wind_speed_10m") ?? 0, c.GetDoubleOrNull("wind_gusts_10m") ?? 0);

    var hourly = new List<WeatherHourDto>();
    if (root.GetOrNull("hourly") is { } h)
    {
      var times = Numbers(h, "time");
      var temps = Numbers(h, "temperature_2m");
      var rains = Numbers(h, "precipitation");
      var codes = Numbers(h, "weather_code");
      var gusts = Numbers(h, "wind_gusts_10m");
      for (var i = 0; i < times.Count; i++)
      {
        if (times[i] is not { } ts) continue;
        var hc = (int)(At(codes, i) ?? 0);
        hourly.Add(new WeatherHourDto(FromUnix(ts), At(temps, i) ?? 0, At(rains, i) ?? 0, hc, WeatherCodes.Describe(hc), At(gusts, i) ?? 0));
      }
    }
    return new WeatherForecast(current, hourly.OrderBy(x => x.Time).ToList());
  }

  private static DateTime FromUnix(double seconds) => DateTimeOffset.FromUnixTimeSeconds((long)seconds).UtcDateTime;

  private static double? At(List<double?> list, int i) => i < list.Count ? list[i] : null;

  /// <summary>Mảng số của property; phần tử null/sai kiểu -> null.</summary>
  private static List<double?> Numbers(JsonElement parent, string name) =>
    parent.GetArray(name).Select(e => e.ValueKind == JsonValueKind.Number && e.TryGetDouble(out var d) ? (double?)d : null).ToList();
}
