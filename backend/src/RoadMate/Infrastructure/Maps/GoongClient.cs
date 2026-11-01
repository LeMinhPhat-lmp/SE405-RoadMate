using System.Text.Json;

namespace RoadMate.Infrastructure.Maps;

public class GoongClient(HttpClient http, IOptions<GoongOptions> options, ILogger<GoongClient> log) : IGoongClient
{
  private const int MaxAttempts = 2;   // 1 lần thử lại cho lỗi mạng/5xx (GET idempotent)

  public static readonly Error KeyRejected = Error.External("MAP_KEY_REJECTED",
    "Goong từ chối API key (kiểm tra Goong:ApiKey phải là REST API key còn hiệu lực).");
  public static readonly Error QuotaExceeded = new("MAP_QUOTA_EXCEEDED",
    "Đã vượt hạn mức/tần suất gọi Goong, hãy thử lại sau.", ErrorType.TooManyRequests);

  public async Task<Result<JsonElement>> GetAsync(string path, GoongQuery query, CancellationToken ct)
  {
    var o = options.Value;
    if (string.IsNullOrWhiteSpace(o.ApiKey))
      return Error.NotConfigured("MAP_NOT_CONFIGURED", "Chưa cấu hình Goong:ApiKey (biến môi trường Goong__ApiKey hoặc GOONG_API_KEY trong .env).");

    // URL chứa api_key: KHÔNG được log URL, và message exception cũng không được chứa URL.
    var url = $"{o.BaseUrl.TrimEnd('/')}/{path.TrimStart('/')}?{query.Build()}&api_key={Uri.EscapeDataString(o.ApiKey.Trim())}";

    for (var attempt = 1; ; attempt++)
    {
      try
      {
        using var res = await http.GetAsync(url, ct);
        if ((int)res.StatusCode >= 500 && attempt < MaxAttempts) { await Task.Delay(300, ct); continue; }
        return await ReadAsync(res, path, ct);
      }
      catch (HttpRequestException ex) when (attempt < MaxAttempts)
      {
        log.LogWarning("Goong {Path}: lỗi mạng ({Type}), thử lại.", path, ex.GetType().Name);
        await Task.Delay(300, ct);
      }
      catch (HttpRequestException ex)
      {
        log.LogWarning("Goong {Path}: không kết nối được ({Type}).", path, ex.GetType().Name);
        return Error.External("MAP_UNREACHABLE", "Không kết nối được tới Goong.");
      }
      catch (OperationCanceledException) when (!ct.IsCancellationRequested)
      {
        log.LogWarning("Goong {Path}: timeout sau {Seconds}s.", path, o.TimeoutSeconds);
        return Error.External("MAP_TIMEOUT", "Goong phản hồi quá chậm (timeout).");
      }
    }
  }

  private async Task<Result<JsonElement>> ReadAsync(HttpResponseMessage res, string path, CancellationToken ct)
  {
    JsonElement? root = null;
    try
    {
      await using var stream = await res.Content.ReadAsStreamAsync(ct);
      using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
      root = doc.RootElement.Clone();
    }
    catch (JsonException) { /* body không phải JSON, xử lý bên dưới */ }

    var providerStatus = root?.GetStringOrNull("status");
    if (res.IsSuccessStatusCode)
    {
      if (root is null) return Error.External("MAP_PROVIDER_ERROR", "Goong trả về dữ liệu không phải JSON.");
      var statusError = FromProviderStatus(providerStatus, path);
      return statusError is null ? root.Value : statusError;
    }

    // HTTP lỗi nhưng body vẫn là "không có kết quả" hợp lệ -> để service xử lý như ZERO_RESULTS.
    if (root is not null && providerStatus is "ZERO_RESULTS" or "NOT_FOUND") return root.Value;

    log.LogWarning("Goong {Path}: HTTP {Status} status={ProviderStatus}.", path, (int)res.StatusCode, providerStatus ?? "-");
    return FromProviderStatus(providerStatus, path) ?? (int)res.StatusCode switch
    {
      401 or 403 => KeyRejected,
      429 => QuotaExceeded,
      400 => Error.External("MAP_BAD_REQUEST", "Goong báo yêu cầu không hợp lệ."),
      var code => Error.External("MAP_PROVIDER_ERROR", $"Goong trả về HTTP {code}.")
    };
  }

  /// <summary>Đổi trường "status" của Goong thành Error; null nếu OK hoặc "không có kết quả" (service tự xử lý).</summary>
  private Error? FromProviderStatus(string? status, string path)
  {
    switch (status?.ToUpperInvariant())
    {
      case null or "" or "OK" or "ZERO_RESULTS" or "NOT_FOUND": return null;
      case "REQUEST_DENIED" or "INVALID_KEY" or "UNAUTHORIZED" or "FORBIDDEN": return KeyRejected;
      case "OVER_QUERY_LIMIT" or "OVER_DAILY_LIMIT" or "RESOURCE_EXHAUSTED" or "TOO_MANY_REQUESTS": return QuotaExceeded;
      case "INVALID_REQUEST": return Error.External("MAP_BAD_REQUEST", "Goong báo yêu cầu không hợp lệ (INVALID_REQUEST).");
      default:
        log.LogWarning("Goong {Path}: status lạ {Status}.", path, status);
        return Error.External("MAP_PROVIDER_ERROR", $"Goong trả về trạng thái {status}.");
    }
  }
}
