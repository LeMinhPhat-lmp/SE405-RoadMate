namespace RoadMate.Repositories.Interfaces;

public interface ITripRepository : IRepository<Trip>, ISoftDeleteRepository<Trip>
{
  Task<Trip?> GetOwnedAsync(Guid id, Guid userId, CancellationToken ct = default);
  Task<Trip?> GetOwnedWithDetailsAsync(Guid id, Guid userId, CancellationToken ct = default);
  Task<List<Trip>> ListByOwnerAsync(Guid userId, string? status, CancellationToken ct = default);
  Task<bool> HasActiveAsync(Guid userId, CancellationToken ct = default);
  Task<Trip?> GetByTrackingTokenAsync(string token, CancellationToken ct = default);
}
