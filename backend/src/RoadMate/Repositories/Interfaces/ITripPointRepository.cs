namespace RoadMate.Repositories.Interfaces;

public interface ITripPointRepository : IRepository<TripPoint>
{
  Task<List<TripPoint>> ListByTripAsync(Guid tripId, CancellationToken ct = default);
  Task<List<TripPoint>> ListRecentAsync(Guid tripId, int take, CancellationToken ct = default);
}
