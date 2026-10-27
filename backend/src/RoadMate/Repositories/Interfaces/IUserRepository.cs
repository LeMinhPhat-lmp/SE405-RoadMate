namespace RoadMate.Repositories.Interfaces;

public interface IUserRepository : IRepository<User>
{
  Task<User?> GetByUidAsync(string uid, CancellationToken ct = default);
  Task<User?> GetByPhoneAsync(string phone, CancellationToken ct = default);
}
