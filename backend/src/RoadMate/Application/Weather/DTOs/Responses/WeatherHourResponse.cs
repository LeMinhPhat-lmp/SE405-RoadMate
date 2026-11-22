namespace RoadMate.Application.Weather.DTOs.Responses;

public record WeatherHourResponse(DateTime Time, double TemperatureC, double PrecipitationMm, int WeatherCode, string Description, double GustKmh);
