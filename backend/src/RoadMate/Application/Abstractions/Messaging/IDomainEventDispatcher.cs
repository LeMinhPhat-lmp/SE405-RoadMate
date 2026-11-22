using RoadMate.Domain.Abstractions;
using RoadMate.Domain.Alerts;
using RoadMate.Domain.Common;

namespace RoadMate.Application.Abstractions.Messaging;

public interface IDomainEventDispatcher
{
  Task DispatchAsync(IReadOnlyCollection<IDomainEvent> events, CancellationToken ct);
}
