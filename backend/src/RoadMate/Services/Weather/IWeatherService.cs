namespace RoadMate.Services.Weather;

public interface IWeatherService
{
  /// <summary>Dự báo đã phân tích cho một ô lưới (cache CacheMinutes). Dùng cho cả API và job theo dõi.</summary>
  Task<Result<WeatherForecast>> GetForecastAsync(double lat, double lng, CancellationToken ct);
  /// <summary>Thời tiết hiện tại + vài giờ tới + cảnh báo tại một điểm.</summary>
  Task<Result<WeatherDto>> GetAsync(double lat, double lng, CancellationToken ct);
  /// <summary>Thời tiết tại điểm đi, các điểm dừng và điểm đến của chuyến.</summary>
  Task<Result<TripWeatherDto>> GetForTripAsync(Guid userId, Guid tripId, CancellationToken ct);
}
