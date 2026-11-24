using RoadMate.Application.Abstractions.Messaging;
using RoadMate.Domain.Abstractions;

namespace RoadMate.Infrastructure.Persistence;

/// <summary>In-process dispatcher: resolves every <c>IDomainEventHandler&lt;T&gt;</c> registered for the event's runtime type (no MediatR).</summary>
public class DomainEventDispatcher(IServiceProvider services) : IDomainEventDispatcher
{
  public async Task DispatchAsync(IReadOnlyCollection<IDomainEvent> events, CancellationToken ct)
  {
    foreach (var e in events)
    {
      var handlerType = typeof(IDomainEventHandler<>).MakeGenericType(e.GetType());
      var method = handlerType.GetMethod(nameof(IDomainEventHandler<IDomainEvent>.HandleAsync))!;
      foreach (var handler in services.GetServices(handlerType))
        await (Task)method.Invoke(handler, [e, ct])!;
    }
  }
}
