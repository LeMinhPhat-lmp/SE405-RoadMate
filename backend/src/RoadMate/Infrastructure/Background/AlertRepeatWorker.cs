using RoadMate.Services.Alerts;

namespace RoadMate.Infrastructure.Background;

/// <summary>Lặp gửi cảnh báo đến hạn (phương án dự phòng khi app offline).</summary>
public class AlertRepeatWorker(IServiceScopeFactory scopes, ILogger<AlertRepeatWorker> log, IOptions<AlertOptions> options)
  : PeriodicWorker(scopes, log)
{
  protected override string Name => nameof(AlertRepeatWorker);
  protected override TimeSpan Interval => TimeSpan.FromSeconds(Math.Max(5, options.Value.WorkerPollSeconds));
  protected override Task RunAsync(IServiceProvider services, CancellationToken ct) =>
    services.GetRequiredService<IAlertRepeatProcessor>().ProcessDueAsync(ct);
}
