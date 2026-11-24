using RoadMate.Api.Workers;

namespace RoadMate.Api.Startup;

public static class WorkerExtensions
{
    /// <summary>Background jobs run in the API process. The outbox/repeat claims are lease-based (safe with several instances);
    /// the in-memory cooldowns of the group/weather monitors are per instance.</summary>
    public static IServiceCollection AddAppWorkers(this IServiceCollection s) => s
      .AddHostedService<OutboxWorker>()
      .AddHostedService<AlertRepeatWorker>()
      .AddHostedService<TripMonitorWorker>()
      .AddHostedService<GroupMonitorWorker>()
      .AddHostedService<WeatherWatchWorker>();
}
