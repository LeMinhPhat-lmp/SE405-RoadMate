using RoadMate.Domain.Abstractions;

namespace RoadMate.Domain.Events;

public sealed record LostSignalRaised(Guid TripId, DateTimeOffset OccurredAt) : IDomainEvent;
