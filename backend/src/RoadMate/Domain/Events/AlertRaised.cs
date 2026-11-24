using RoadMate.Domain.Abstractions;

namespace RoadMate.Domain.Events;

public sealed record AlertRaised(Guid AlertId, Guid TripId, DateTimeOffset OccurredAt) : IDomainEvent;
