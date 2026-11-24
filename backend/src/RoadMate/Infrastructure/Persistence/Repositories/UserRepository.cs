using Microsoft.EntityFrameworkCore;
using RoadMate.Application.Abstractions.Identity;
using RoadMate.Application.Abstractions.Persistence;
using RoadMate.Domain.Users;

namespace RoadMate.Infrastructure.Persistence.Repositories;

public class UserRepository(AppDbContext db, ICurrentUser user, TimeProvider clock) : SoftDeleteRepositoryBase<User>(db, user, clock), IUserRepository
{
  public Task<User?> GetWithContactsAsync(Guid id, CancellationToken ct = default) => Set.Include(u => u.Contacts).FirstOrDefaultAsync(u => u.Id == id, ct);
  public Task<User?> GetByFirebaseUidAsync(string uid, CancellationToken ct = default) => Set.FirstOrDefaultAsync(u => u.FirebaseUid == uid, ct);

  public async Task<Guid?> GetIdByFirebaseUidAsync(string uid, CancellationToken ct = default) =>
    await Set.AsNoTracking().Where(u => u.FirebaseUid == uid).Select(u => (Guid?)u.Id).FirstOrDefaultAsync(ct);

  public async Task<Guid?> FindIdByVerifiedPhoneAsync(string phone, CancellationToken ct = default) =>
    await Set.AsNoTracking().Where(u => u.Phone == phone).Select(u => (Guid?)u.Id).FirstOrDefaultAsync(ct);

  public async Task<IReadOnlyDictionary<Guid, string>> GetFcmTokensAsync(IReadOnlyCollection<Guid> ids, CancellationToken ct = default) =>
    await Set.AsNoTracking().Where(u => ids.Contains(u.Id) && u.FcmToken != null && u.FcmToken != "").ToDictionaryAsync(u => u.Id, u => u.FcmToken!, ct);
}
