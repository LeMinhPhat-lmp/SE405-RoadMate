namespace RoadMate.Repositories.Interfaces;

public interface IMemoryRepository : IRepository<Memory>, ISoftDeleteRepository<Memory>
{
  Task<List<Memory>> ListByTripAsync(Guid tripId, CancellationToken ct = default);
  Task<Memory?> GetOwnedAsync(Guid id, Guid userId, CancellationToken ct = default);
}
