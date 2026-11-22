using RoadMate.Domain.Trips;
using RoadMate.Domain.ValueObjects;

namespace RoadMate.Application.Abstractions.Persistence;

public interface IMemoryRepository : IRepository<Memory>, ISoftDeleteRepository<Memory>
{
  /// <summary>Tracked memory that belongs to a trip owned by <paramref name="ownerId"/>.</summary>
  Task<Memory?> GetOwnedAsync(Guid memoryId, Guid ownerId, CancellationToken ct = default);
  /// <summary>No tracking, ordered by TakenAt.</summary>
  Task<IReadOnlyList<Memory>> ListByTripAsync(Guid tripId, CancellationToken ct = default);
}
