using Microsoft.Extensions.Options;
using RoadMate.Application.Abstractions.Messaging;
using RoadMate.Application.Abstractions.Persistence;
using RoadMate.Application.Alerts.Interfaces;
using RoadMate.Domain.Alerts;
using RoadMate.Domain.Trips;

namespace RoadMate.Application.Alerts.Implements;

/// <summary>Leases due alerts atomically (safe with several instances), then repeats each in its OWN scope so one bad alert cannot block the rest.</summary>
public class AlertRepeatProcessor(IAlertRepository alerts, IIsolatedExecutor isolated, IOptions<AlertOptions> options, TimeProvider clock, ILogger<AlertRepeatProcessor> log) : IAlertRepeatProcessor
{
  public async Task<int> ProcessDueAsync(string workerId, CancellationToken ct)
  {
    var o = options.Value;
    var ids = await alerts.ClaimDueForRepeatAsync(workerId, clock.GetUtcNow(), TimeSpan.FromSeconds(Math.Max(30, o.WorkerPollSeconds * 4)), 20, ct);
    foreach (var id in ids)
    {
      try { await isolated.RunAsync<IAlertRepeatService>(s => s.RepeatAsync(id, ct), ct); }
      catch (Exception ex) when (ex is not OperationCanceledException) { log.LogError(ex, "Lặp cảnh báo {AlertId} lỗi, sẽ thử lại sau", id); }
    }
    return ids.Count;
  }
}
