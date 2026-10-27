namespace RoadMate.Repositories;

public interface IRepository<T> where T : class
{
  Task<T?> GetByIdAsync(object id, CancellationToken ct = default);
  Task AddAsync(T entity, CancellationToken ct = default);
  Task AddRangeAsync(IEnumerable<T> entities, CancellationToken ct = default);
  void Update(T entity);
  void Remove(T entity);
}

public interface IUserRepository : IRepository<User>
{
  Task<User?> GetByUidAsync(string uid, CancellationToken ct = default);
  Task<User?> GetByPhoneAsync(string phone, CancellationToken ct = default);
}

public interface IEmergencyContactRepository : IRepository<EmergencyContact>
{
  Task<List<EmergencyContact>> ListByUserAsync(string uid, CancellationToken ct = default);
  Task<int> CountByUserAsync(string uid, CancellationToken ct = default);
  Task<EmergencyContact?> GetOwnedAsync(Guid id, string uid, CancellationToken ct = default);
  Task<List<EmergencyContact>> GetByIdsAsync(IEnumerable<Guid> ids, string uid, CancellationToken ct = default);
  Task<List<EmergencyContact>> ListByPhoneAsync(string phone, CancellationToken ct = default);
  /// <summary>Người nhận cảnh báo của chuyến; nếu chuyến chưa chọn thì lấy toàn bộ liên hệ của user.</summary>
  Task<List<EmergencyContact>> GetAlertRecipientsAsync(Guid tripId, string uid, CancellationToken ct = default);
}

public interface ITripRepository : IRepository<Trip>
{
  Task<Trip?> GetOwnedAsync(Guid id, string uid, CancellationToken ct = default);
  Task<Trip?> GetOwnedWithDetailsAsync(Guid id, string uid, CancellationToken ct = default);
  Task<List<Trip>> ListByOwnerAsync(string uid, string? status, CancellationToken ct = default);
  Task<bool> HasActiveAsync(string uid, CancellationToken ct = default);
  Task<Trip?> GetByTrackingTokenAsync(string token, CancellationToken ct = default);
}

public interface ITripPointRepository : IRepository<TripPoint>
{
  Task<List<TripPoint>> ListByTripAsync(Guid tripId, CancellationToken ct = default);
  Task<List<TripPoint>> ListRecentAsync(Guid tripId, int take, CancellationToken ct = default);
}

public interface IMemoryRepository : IRepository<Memory>
{
  Task<List<Memory>> ListByTripAsync(Guid tripId, CancellationToken ct = default);
  Task<Memory?> GetOwnedAsync(Guid id, string uid, CancellationToken ct = default);
}

public interface IAlertRepository : IRepository<Alert>
{
  Task<Alert?> GetByClientIdAsync(string clientId, CancellationToken ct = default);
  Task<Alert?> GetOwnedAsync(Guid id, string uid, CancellationToken ct = default);
  Task<List<Alert>> ListByTripAsync(Guid tripId, CancellationToken ct = default);
  Task<List<Alert>> ListOpenByTripAsync(Guid tripId, CancellationToken ct = default);
  Task<int> CountByTripAsync(Guid tripId, CancellationToken ct = default);
  Task<List<Alert>> ListDueForRepeatAsync(DateTime now, int take, CancellationToken ct = default);
}

public interface IAlertDeliveryRepository : IRepository<AlertDelivery> { }

public interface IUnitOfWork : IDisposable
{
  IUserRepository Users { get; }
  IEmergencyContactRepository EmergencyContacts { get; }
  ITripRepository Trips { get; }
  ITripPointRepository TripPoints { get; }
  IMemoryRepository Memories { get; }
  IAlertRepository Alerts { get; }
  IAlertDeliveryRepository AlertDeliveries { get; }

  Task<int> SaveChangesAsync(CancellationToken ct = default);
  Task ExecuteInTransactionAsync(Func<CancellationToken, Task> action, CancellationToken ct = default);
}
