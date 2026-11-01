using System.Text.Json;

namespace RoadMate.Infrastructure.Maps;

public class GoongClient(HttpClient http, IOptions<GoongOptions> options) : IGoongClient
{
  public async Task<Result<JsonElement>> GetAsync(string path, string query, CancellationToken ct)
  {
    if (string.IsNullOrWhiteSpace(options.Value.ApiKey)) return Error.NotConfigured("MAP_NOT_CONFIGURED", "Chưa cấu hình Goong:ApiKey.");
    try
    {
      var url = $"{options.Value.BaseUrl.TrimEnd('/')}/{path}?{query}&api_key={Uri.EscapeDataString(options.Value.ApiKey)}";
      using var res = await http.GetAsync(url, ct);
      if (!res.IsSuccessStatusCode) return Error.External("MAP_PROVIDER_ERROR", $"Goong trả về HTTP {(int)res.StatusCode}.");
      using var doc = await JsonDocument.ParseAsync(await res.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
      return doc.RootElement.Clone();
    }
    catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
    {
      return Error.External("MAP_PROVIDER_ERROR", ex.Message);
    }
  }
}
