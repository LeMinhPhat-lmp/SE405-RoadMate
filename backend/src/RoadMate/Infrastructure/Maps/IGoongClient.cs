using System.Text.Json;

namespace RoadMate.Infrastructure.Maps;

/// <summary>Cổng duy nhất ra Goong Maps: giữ API key phía server, gom lỗi mạng/HTTP thành Result.</summary>
public interface IGoongClient
{
  Task<Result<JsonElement>> GetAsync(string path, string query, CancellationToken ct);
}
