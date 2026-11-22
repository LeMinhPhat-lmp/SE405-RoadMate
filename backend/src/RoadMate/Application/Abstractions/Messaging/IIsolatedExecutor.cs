using RoadMate.Domain.Abstractions;
using RoadMate.Domain.Alerts;
using RoadMate.Domain.Common;

namespace RoadMate.Application.Abstractions.Messaging;

/// <summary>Runs work in a FRESH DI scope (own DbContext + change tracker) so one bad item never poisons the others.</summary>
public interface IIsolatedExecutor
{
  Task RunAsync<TService>(Func<TService, Task> work, CancellationToken ct) where TService : notnull;
}
