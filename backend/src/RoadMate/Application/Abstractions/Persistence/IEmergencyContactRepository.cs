using RoadMate.Domain.Users;

namespace RoadMate.Application.Abstractions.Persistence;

public interface IEmergencyContactRepository
{
  /// <summary>No tracking, ordered by priority.</summary>
  Task<IReadOnlyList<EmergencyContact>> ListForUserAsync(Guid userId, CancellationToken ct = default);
  Task<EmergencyContact?> GetOwnedAsync(Guid userId, Guid contactId, CancellationToken ct = default);
  /// <summary>Of <paramref name="ids"/>, those that belong to <paramref name="userId"/> (and are not deleted).</summary>
  Task<IReadOnlyList<Guid>> FilterOwnedIdsAsync(Guid userId, IReadOnlyCollection<Guid> ids, CancellationToken ct = default);
  Task<IReadOnlyList<AlertRecipient>> GetAlertRecipientsAsync(Guid tripId, Guid ownerId, CancellationToken ct = default);
  /// <summary>Links (LinkedUserId) every not-yet-linked contact whose phone matches; returns how many were linked.</summary>
  Task<int> LinkByPhoneAsync(string normalizedPhone, Guid userId, CancellationToken ct = default);
}
