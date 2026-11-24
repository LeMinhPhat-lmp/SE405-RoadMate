namespace RoadMate.Application.Weather.DTOs.Responses;

/// <param name="Code">HEAVY_RAIN | STRONG_WIND | THUNDERSTORM.</param>
/// <param name="At">Thời điểm (UTC) được dự báo xảy ra sớm nhất.</param>
public record WeatherWarningResponse(string Code, string Message, DateTime At);
