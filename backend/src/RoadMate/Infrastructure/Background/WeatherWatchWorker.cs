using RoadMate.Infrastructure.Weather;
using RoadMate.Services.Weather;

namespace RoadMate.Infrastructure.Background;

/// <summary>Nhắc chủ chuyến khi phía trước có mưa to/gió mạnh/giông.</summary>
public class WeatherWatchWorker(IServiceScopeFactory scopes, ILogger<WeatherWatchWorker> log, IOptions<WeatherOptions> options)
  : PeriodicWorker(scopes, log)
{
  protected override string Name => nameof(WeatherWatchWorker);
  protected override TimeSpan Interval => TimeSpan.FromMinutes(Math.Max(1, options.Value.WatchIntervalMinutes));
  protected override Task RunAsync(IServiceProvider services, CancellationToken ct) =>
    services.GetRequiredService<IWeatherWatchService>().RunOnceAsync(ct);
}
