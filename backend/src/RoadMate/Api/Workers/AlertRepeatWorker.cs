using RoadMate.Application.Alerts;
using Microsoft.Extensions.Options;
using RoadMate.Application.Alerts.Interfaces;

namespace RoadMate.Api.Workers;

/// <summary>Lặp gửi cảnh báo đến hạn (phương án dự phòng khi app offline).</summary>
public class AlertRepeatWorker(IServiceScopeFactory scopes, ILogger<AlertRepeatWorker> log, IOptions<AlertOptions> options)
  : PeriodicWorker(scopes, log)
{
  private readonly string _workerId = WorkerIdentity.New("repeat");
  protected override string Name => nameof(AlertRepeatWorker);
  protected override TimeSpan Interval => TimeSpan.FromSeconds(Math.Max(5, options.Value.WorkerPollSeconds));
  protected override Task RunAsync(IServiceProvider services, CancellationToken ct) =>
    services.GetRequiredService<IAlertRepeatProcessor>().ProcessDueAsync(_workerId, ct);
}
