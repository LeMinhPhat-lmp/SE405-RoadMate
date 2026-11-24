using RoadMate.Domain.Abstractions;
using RoadMate.Domain.ValueObjects;

namespace RoadMate.Domain.Events;

public sealed record LocationReported(Guid TripId, Guid OwnerId, Guid? GroupId, GeoPoint Location, int? Battery, bool ClearsLostSignal, DateTimeOffset OccurredAt) : IDomainEvent;
