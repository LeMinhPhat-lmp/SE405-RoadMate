using RoadMate.Services.Monitoring;

namespace RoadMate.Infrastructure.Background;

/// <summary>Phát hiện mất tín hiệu và về trễ cho các chuyến đang chạy.</summary>
public class TripMonitorWorker(IServiceScopeFactory scopes, ILogger<TripMonitorWorker> log, IOptions<SafetyOptions> options)
  : PeriodicWorker(scopes, log)
{
  protected override string Name => nameof(TripMonitorWorker);
  protected override TimeSpan Interval => TimeSpan.FromSeconds(Math.Max(5, options.Value.MonitorIntervalSeconds));
  protected override Task RunAsync(IServiceProvider services, CancellationToken ct) =>
    services.GetRequiredService<ITripMonitorService>().RunOnceAsync(ct);
}
