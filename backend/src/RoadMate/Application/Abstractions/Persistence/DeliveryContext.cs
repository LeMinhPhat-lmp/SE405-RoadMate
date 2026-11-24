using RoadMate.Application.Alerts;
using RoadMate.Application.Tracking;
using RoadMate.Domain.Alerts;
using RoadMate.Domain.Users;

namespace RoadMate.Application.Abstractions.Persistence;

/// <summary>Everything the sender needs to render and deliver one outbox row.</summary>
public sealed record DeliveryContext(Alert Alert, User Owner, Guid? ContactId, string? Phone, string? AckToken, string? PushToken);
