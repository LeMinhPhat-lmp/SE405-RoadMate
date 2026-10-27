namespace RoadMate.Repositories.Interfaces;

public interface IAlertRepository : IRepository<Alert>
{
  Task<Alert?> GetByClientIdAsync(string clientId, CancellationToken ct = default);
  Task<Alert?> GetOwnedAsync(Guid id, string uid, CancellationToken ct = default);
  Task<List<Alert>> ListByTripAsync(Guid tripId, CancellationToken ct = default);
  Task<List<Alert>> ListOpenByTripAsync(Guid tripId, CancellationToken ct = default);
  Task<int> CountByTripAsync(Guid tripId, CancellationToken ct = default);
  Task<List<Alert>> ListDueForRepeatAsync(DateTime now, int take, CancellationToken ct = default);
}
