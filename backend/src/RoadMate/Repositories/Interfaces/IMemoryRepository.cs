namespace RoadMate.Repositories.Interfaces;

public interface IMemoryRepository : IRepository<Memory>
{
  Task<List<Memory>> ListByTripAsync(Guid tripId, CancellationToken ct = default);
  Task<Memory?> GetOwnedAsync(Guid id, string uid, CancellationToken ct = default);
}
