using System.Text.Json;
using RoadMate.Domain.Common;

namespace RoadMate.Application.Abstractions.Weather;

/// <summary>Cổng duy nhất ra Open-Meteo: gom lỗi mạng/HTTP/quota thành Result. Dữ liệu thô, service tự phân tích.</summary>
public interface IWeatherClient
{
  Task<Result<JsonElement>> GetForecastAsync(double lat, double lng, CancellationToken ct);
}
