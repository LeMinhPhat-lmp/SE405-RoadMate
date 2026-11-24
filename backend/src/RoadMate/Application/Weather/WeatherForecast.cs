using RoadMate.Application.Weather.DTOs.Responses;

namespace RoadMate.Application.Weather;

/// <summary>Dự báo đã phân tích cho một ô lưới: thời tiết hiện tại + từng giờ (UTC, tăng dần). Là đối tượng được cache.</summary>
public record WeatherForecast(WeatherNowResponse Current, List<WeatherHourResponse> Hourly);
