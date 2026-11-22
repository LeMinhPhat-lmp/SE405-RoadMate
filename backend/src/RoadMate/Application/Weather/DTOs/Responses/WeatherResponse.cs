namespace RoadMate.Application.Weather.DTOs.Responses;

public record WeatherResponse(double Lat, double Lng, WeatherNowResponse Current, List<WeatherHourResponse> Hourly, List<WeatherWarningResponse> Warnings);
