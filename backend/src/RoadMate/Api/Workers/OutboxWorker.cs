using Microsoft.Extensions.Options;
using RoadMate.Application.Alerts;
using RoadMate.Application.Alerts.Interfaces;

namespace RoadMate.Api.Workers;

/// <summary>Sends queued notifications (the outbox). Several instances are safe: rows are claimed with FOR UPDATE SKIP LOCKED.</summary>
public class OutboxWorker(IServiceScopeFactory scopes, ILogger<OutboxWorker> log, IOptions<AlertOptions> options) : PeriodicWorker(scopes, log)
{
  private readonly string _workerId = WorkerIdentity.New("outbox");
  protected override string Name => nameof(OutboxWorker);
  protected override TimeSpan Interval => TimeSpan.FromSeconds(Math.Max(2, Math.Min(5, options.Value.WorkerPollSeconds)));
  protected override Task RunAsync(IServiceProvider services, CancellationToken ct) =>
    services.GetRequiredService<IOutboxProcessor>().ProcessBatchAsync(_workerId, ct);
}
