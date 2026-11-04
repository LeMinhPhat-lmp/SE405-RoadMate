namespace RoadMate.Services.Weather;

/// <summary>Dự báo đã phân tích cho một ô lưới: thời tiết hiện tại + từng giờ (UTC, tăng dần). Là đối tượng được cache.</summary>
public record WeatherForecast(WeatherNowDto Current, List<WeatherHourDto> Hourly);
