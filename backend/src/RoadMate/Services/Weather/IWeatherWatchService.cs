namespace RoadMate.Services.Weather;

/// <summary>Theo dõi thời tiết các điểm phía trước của chuyến đang chạy và nhắc chủ chuyến khi có mưa to/gió mạnh/giông.</summary>
public interface IWeatherWatchService
{
  Task RunOnceAsync(CancellationToken ct);
}
