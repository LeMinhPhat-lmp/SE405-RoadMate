using RoadMate.Domain.Abstractions;

namespace RoadMate.Domain.Events;

public sealed record TripFinished(Guid TripId, Guid OwnerId, DateTimeOffset OccurredAt) : IDomainEvent;
