namespace RoadMate.Repositories.Interfaces;

public interface IUserRepository : IRepository<User>, ISoftDeleteRepository<User>
{
  Task<User?> GetByFirebaseUidAsync(string firebaseUid, CancellationToken ct = default);
  Task<Guid?> GetUserIdByFirebaseUidAsync(string firebaseUid, CancellationToken ct = default);
  Task<User?> GetByPhoneAsync(string phone, CancellationToken ct = default);
}
