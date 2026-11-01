using System.Text.Json;

namespace RoadMate.Infrastructure.Maps;

/// <summary>
/// Cổng duy nhất ra Goong REST API V2: giữ API key phía server, gom lỗi mạng/HTTP/status của Goong thành Result.
/// Các status "ZERO_RESULTS"/"NOT_FOUND" KHÔNG bị coi là lỗi ở tầng này (service tự quyết định ý nghĩa).
/// </summary>
public interface IGoongClient
{
  /// <param name="path">Đường dẫn tương đối, vd "v2/place/autocomplete".</param>
  Task<Result<JsonElement>> GetAsync(string path, GoongQuery query, CancellationToken ct);
}
