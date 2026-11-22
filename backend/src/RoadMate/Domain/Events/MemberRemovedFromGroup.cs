using RoadMate.Domain.Abstractions;

namespace RoadMate.Domain.Events;

public sealed record MemberRemovedFromGroup(Guid GroupId, Guid UserId, DateTimeOffset OccurredAt) : IDomainEvent;
