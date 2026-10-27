using RoadMate.Data;

namespace RoadMate.Repositories.Implements;

public class UserRepository(AppDbContext context, ICurrentUserContext currentUser)
  : SoftDeleteRepository<User>(context, currentUser), IUserRepository
{
  public Task<User?> GetByFirebaseUidAsync(string firebaseUid, CancellationToken ct = default) =>
    Set.FirstOrDefaultAsync(u => u.FirebaseUid == firebaseUid, ct);
  public async Task<Guid?> GetUserIdByFirebaseUidAsync(string firebaseUid, CancellationToken ct = default) =>
    await Set.AsNoTracking().Where(u => u.FirebaseUid == firebaseUid).Select(u => (Guid?)u.Id).FirstOrDefaultAsync(ct);
  public Task<User?> GetByPhoneAsync(string phone, CancellationToken ct = default) =>
    Set.FirstOrDefaultAsync(u => u.Phone == phone, ct);
}
