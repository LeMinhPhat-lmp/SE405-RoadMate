namespace RoadMate.Dtos.Weather;

/// <param name="WeatherCode">Mã WMO của Open-Meteo.</param>
public record WeatherNowDto(DateTime Time, double TemperatureC, double PrecipitationMm, int WeatherCode, string Description, double WindKmh, double GustKmh);
public record WeatherHourDto(DateTime Time, double TemperatureC, double PrecipitationMm, int WeatherCode, string Description, double GustKmh);
/// <param name="Code">HEAVY_RAIN | STRONG_WIND | THUNDERSTORM.</param>
/// <param name="At">Thời điểm (UTC) được dự báo xảy ra sớm nhất.</param>
public record WeatherWarningDto(string Code, string Message, DateTime At);
public record WeatherDto(double Lat, double Lng, WeatherNowDto Current, List<WeatherHourDto> Hourly, List<WeatherWarningDto> Warnings);

/// <param name="Kind">origin | waypoint | destination.</param>
public record TripWeatherPointDto(string Name, string Kind, WeatherDto Weather);
/// <param name="FailedCount">Số điểm không lấy được dự báo (các điểm còn lại vẫn trả về).</param>
public record TripWeatherDto(Guid TripId, List<TripWeatherPointDto> Points, int FailedCount);
