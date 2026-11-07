using System.Text.Json;

namespace RoadMate.Infrastructure.Weather;

public class OpenMeteoClient(HttpClient http, IOptions<WeatherOptions> options, ILogger<OpenMeteoClient> log) : IWeatherClient
{
  private const int MaxAttempts = 2;   // 1 lần thử lại cho lỗi mạng/5xx (GET idempotent)

  public async Task<Result<JsonElement>> GetForecastAsync(double lat, double lng, CancellationToken ct)
  {
    var o = options.Value;
    // timeformat=unixtime + timezone=UTC: mọi mốc thời gian là epoch giây, không phụ thuộc múi giờ địa phương.
    var url = $"{o.BaseUrl.TrimEnd('/')}/v1/forecast?latitude={lat.ToCoordinate()}&longitude={lng.ToCoordinate()}" +
      "&current=temperature_2m,precipitation,weather_code,wind_speed_10m,wind_gusts_10m" +
      "&hourly=temperature_2m,precipitation,weather_code,wind_gusts_10m" +
      "&forecast_days=2&timezone=UTC&timeformat=unixtime";

    for (var attempt = 1; ; attempt++)
    {
      try
      {
        using var res = await http.GetAsync(url, ct);
        if ((int)res.StatusCode >= 500 && attempt < MaxAttempts) { await Task.Delay(300, ct); continue; }
        return await ReadAsync(res, ct);
      }
      catch (HttpRequestException ex) when (attempt < MaxAttempts)
      {
        log.LogWarning("Open-Meteo: lỗi mạng ({Type}), thử lại.", ex.GetType().Name);
        await Task.Delay(300, ct);
      }
      catch (HttpRequestException ex)
      {
        log.LogWarning("Open-Meteo: không kết nối được ({Type}).", ex.GetType().Name);
        return Error.External("WEATHER_UNREACHABLE", "Không kết nối được tới dịch vụ thời tiết.");
      }
      catch (OperationCanceledException) when (!ct.IsCancellationRequested)
      {
        log.LogWarning("Open-Meteo: timeout sau {Seconds}s.", o.TimeoutSeconds);
        return Error.External("WEATHER_TIMEOUT", "Dịch vụ thời tiết phản hồi quá chậm (timeout).");
      }
    }
  }

  private async Task<Result<JsonElement>> ReadAsync(HttpResponseMessage res, CancellationToken ct)
  {
    JsonElement? root = null;
    try
    {
      await using var stream = await res.Content.ReadAsStreamAsync(ct);
      using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
      root = doc.RootElement.Clone();
    }
    catch (JsonException) { /* body không phải JSON, xử lý bên dưới */ }

    if (res.IsSuccessStatusCode)
      return root is null ? Error.External("WEATHER_PROVIDER_ERROR", "Dịch vụ thời tiết trả về dữ liệu không phải JSON.") : root.Value;

    var reason = root?.GetStringOrNull("reason");
    log.LogWarning("Open-Meteo: HTTP {Status}, reason {Reason}", (int)res.StatusCode, reason ?? "-");
    return (int)res.StatusCode switch
    {
      429 => Error.TooManyRequests("WEATHER_QUOTA_EXCEEDED", "Đã vượt hạn mức gọi dịch vụ thời tiết, hãy thử lại sau."),
      400 => Error.External("WEATHER_BAD_REQUEST", "Dịch vụ thời tiết báo yêu cầu không hợp lệ."),
      var code => Error.External("WEATHER_PROVIDER_ERROR", $"Dịch vụ thời tiết trả về HTTP {code}.")
    };
  }
}
