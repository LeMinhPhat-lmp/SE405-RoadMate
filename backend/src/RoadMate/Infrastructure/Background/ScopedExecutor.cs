using RoadMate.Application.Abstractions.Messaging;

namespace RoadMate.Infrastructure.Background;

/// <summary>Runs work in a fresh scope (own DbContext/change tracker): a failure in one item cannot poison the next.</summary>
public sealed class ScopedExecutor(IServiceScopeFactory scopes) : IIsolatedExecutor
{
  public async Task RunAsync<TService>(Func<TService, Task> work, CancellationToken ct) where TService : notnull
  {
    await using var scope = scopes.CreateAsyncScope();
    await work(scope.ServiceProvider.GetRequiredService<TService>());
  }
}
