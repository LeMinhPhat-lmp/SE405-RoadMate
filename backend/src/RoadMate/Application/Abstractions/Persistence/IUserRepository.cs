using RoadMate.Domain.Users;

namespace RoadMate.Application.Abstractions.Persistence;

public interface IUserRepository : IGenericRepository<User>, ISoftDeleteRepository<User>
{
  /// <summary>Tracked, with non-deleted contacts loaded (needed for the 5-contact rule).</summary>
  Task<User?> GetWithContactsAsync(Guid id, CancellationToken ct = default);
  Task<User?> GetByFirebaseUidAsync(string firebaseUid, CancellationToken ct = default);
  Task<Guid?> GetIdByFirebaseUidAsync(string firebaseUid, CancellationToken ct = default);
  /// <summary>User whose VERIFIED phone equals <paramref name="normalizedPhone"/> (+84…).</summary>
  Task<Guid?> FindIdByVerifiedPhoneAsync(string normalizedPhone, CancellationToken ct = default);
  Task<IReadOnlyDictionary<Guid, string>> GetFcmTokensAsync(IReadOnlyCollection<Guid> userIds, CancellationToken ct = default);
}
