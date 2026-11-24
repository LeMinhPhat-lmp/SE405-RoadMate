using RoadMate.Domain.Trips;
using RoadMate.Domain.ValueObjects;

namespace RoadMate.Application.Abstractions.Persistence;

public interface ITripPointReader
{
  Task<IReadOnlyList<TripPointSample>> ListAsync(Guid tripId, CancellationToken ct = default);
  Task<IReadOnlyList<TripPointSample>> LastAsync(Guid tripId, int take, CancellationToken ct = default);
}
