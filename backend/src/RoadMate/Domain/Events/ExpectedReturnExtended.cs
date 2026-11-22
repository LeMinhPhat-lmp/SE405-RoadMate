using RoadMate.Domain.Abstractions;

namespace RoadMate.Domain.Events;

/// <summary>Raised when the owner's expected return time moves (late-return alerts must close).</summary>
public sealed record ExpectedReturnExtended(Guid TripId, Guid OwnerId, DateTimeOffset OccurredAt) : IDomainEvent;
