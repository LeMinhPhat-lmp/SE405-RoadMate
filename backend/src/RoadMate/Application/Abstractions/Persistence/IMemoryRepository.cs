using RoadMate.Application.Common;
using RoadMate.Domain.Trips;

namespace RoadMate.Application.Abstractions.Persistence;

public interface IMemoryRepository : IGenericRepository<Memory>, ISoftDeleteRepository<Memory>
{
  /// <summary>Tracked memory that belongs to a trip owned by <paramref name="ownerId"/>.</summary>
  Task<Memory?> GetOwnedAsync(Guid memoryId, Guid ownerId, CancellationToken ct = default);
  /// <summary>No tracking, ordered by (TakenAt, Id) for full trip journal aggregation.</summary>
  Task<IReadOnlyList<Memory>> ListByTripAsync(Guid tripId, CancellationToken ct = default);
  /// <summary>No tracking, keyset-paginated by ascending (TakenAt, Id).</summary>
  Task<CursorPage<Memory>> ListByTripPageAsync(Guid tripId, CursorPosition? after, int limit, CancellationToken ct = default);
}
