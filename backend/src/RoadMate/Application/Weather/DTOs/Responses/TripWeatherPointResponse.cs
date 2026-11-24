namespace RoadMate.Application.Weather.DTOs.Responses;

/// <param name="Kind">origin | waypoint | destination.</param>
public record TripWeatherPointResponse(string Name, string Kind, WeatherResponse Weather);
