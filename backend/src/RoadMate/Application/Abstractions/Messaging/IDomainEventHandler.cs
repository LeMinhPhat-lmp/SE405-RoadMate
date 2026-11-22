using RoadMate.Domain.Abstractions;
using RoadMate.Domain.Alerts;
using RoadMate.Domain.Common;

namespace RoadMate.Application.Abstractions.Messaging;

public interface IDomainEventHandler<in TEvent> where TEvent : IDomainEvent
{
  Task HandleAsync(TEvent e, CancellationToken ct);
}
