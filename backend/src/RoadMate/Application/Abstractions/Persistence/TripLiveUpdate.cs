using RoadMate.Domain.Trips;
using RoadMate.Domain.ValueObjects;

namespace RoadMate.Application.Abstractions.Persistence;

public sealed record TripLiveUpdate(Guid TripId, GeoPoint Location, double? Speed, int? Battery, bool NetworkOk, DateTimeOffset ReportedAt, DateTimeOffset ReceivedAt);
