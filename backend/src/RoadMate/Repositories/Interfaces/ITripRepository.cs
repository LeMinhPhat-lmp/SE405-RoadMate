namespace RoadMate.Repositories.Interfaces;

public interface ITripRepository : IRepository<Trip>
{
  Task<Trip?> GetOwnedAsync(Guid id, string uid, CancellationToken ct = default);
  Task<Trip?> GetOwnedWithDetailsAsync(Guid id, string uid, CancellationToken ct = default);
  Task<List<Trip>> ListByOwnerAsync(string uid, string? status, CancellationToken ct = default);
  Task<bool> HasActiveAsync(string uid, CancellationToken ct = default);
  Task<Trip?> GetByTrackingTokenAsync(string token, CancellationToken ct = default);
}
