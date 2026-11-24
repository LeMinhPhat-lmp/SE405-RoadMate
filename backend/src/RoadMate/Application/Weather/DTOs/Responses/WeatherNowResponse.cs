namespace RoadMate.Application.Weather.DTOs.Responses;

/// <param name="WeatherCode">Mã WMO của Open-Meteo.</param>
public record WeatherNowResponse(DateTime Time, double TemperatureC, double PrecipitationMm, int WeatherCode, string Description, double WindKmh, double GustKmh);
