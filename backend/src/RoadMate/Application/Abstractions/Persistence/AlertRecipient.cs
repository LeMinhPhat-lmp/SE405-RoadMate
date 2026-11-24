using RoadMate.Domain.Users;

namespace RoadMate.Application.Abstractions.Persistence;

/// <summary>Who must be notified for an alert: the trip's chosen contacts (else all contacts) plus the linked app user's push token, if any.</summary>
public sealed record AlertRecipient(Guid ContactId, string Name, string Phone, Guid? LinkedUserId, string? LinkedFcmToken);
