using RoadMate.Domain.Abstractions;
using RoadMate.Domain.Alerts;

namespace RoadMate.Domain.Events;

public sealed record AlertClosed(Guid AlertId, Guid TripId, Guid UserId, AlertStatus Status, AlertResolver ResolvedBy, bool WasNotified, DateTimeOffset OccurredAt) : IDomainEvent;
