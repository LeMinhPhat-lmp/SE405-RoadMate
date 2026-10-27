using RoadMate.Data;
using RoadMate.Repositories.Interfaces;

namespace RoadMate.Repositories.Implements;

public class UserRepository(AppDbContext context) : Repository<RoadMate.Entities.User>(context), IUserRepository
{
  public Task<User?> GetByUidAsync(string uid, CancellationToken ct = default) => Set.FirstOrDefaultAsync(u => u.Uid == uid, ct);
  public Task<User?> GetByPhoneAsync(string phone, CancellationToken ct = default) => Set.FirstOrDefaultAsync(u => u.Phone == phone, ct);
}
