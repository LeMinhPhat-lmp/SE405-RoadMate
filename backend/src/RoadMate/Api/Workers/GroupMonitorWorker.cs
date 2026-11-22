using RoadMate.Application.Groups;
using Microsoft.Extensions.Options;
using RoadMate.Application.Groups.Interfaces;

namespace RoadMate.Api.Workers;

/// <summary>Cảnh báo tụt đoàn và dọn tin nhắn nhóm quá hạn.</summary>
public class GroupMonitorWorker(IServiceScopeFactory scopes, ILogger<GroupMonitorWorker> log, IOptions<GroupOptions> options)
  : PeriodicWorker(scopes, log)
{
  protected override string Name => nameof(GroupMonitorWorker);
  protected override TimeSpan Interval => TimeSpan.FromSeconds(Math.Max(5, options.Value.MonitorIntervalSeconds));
  protected override Task RunAsync(IServiceProvider services, CancellationToken ct) =>
    services.GetRequiredService<IGroupMonitorService>().RunOnceAsync(ct);
}
