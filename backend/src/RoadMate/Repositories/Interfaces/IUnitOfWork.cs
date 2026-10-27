namespace RoadMate.Repositories.Interfaces;

public interface IUnitOfWork : IDisposable
{
  IUserRepository UserRepository { get; }
  IEmergencyContactRepository EmergencyContactRepository { get; }
  ITripRepository TripRepository { get; }
  ITripPointRepository TripPointRepository { get; }
  IMemoryRepository MemoryRepository { get; }
  IAlertRepository AlertRepository { get; }
  IAlertDeliveryRepository AlertDeliveryRepository { get; }

  Task<int> SaveChangesAsync(CancellationToken ct = default);
  Task ExecuteInTransactionAsync(Func<CancellationToken, Task> action, CancellationToken ct = default);
}
