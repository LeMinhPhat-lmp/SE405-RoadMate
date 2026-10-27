namespace RoadMate.Repositories.Interfaces;

public interface IUnitOfWork : IDisposable
{
  public IUserRepository UserRepository { get; }
  public IEmergencyContactRepository EmergencyContactRepository { get; }
  public ITripRepository TripRepository { get; }
  public ITripPointRepository TripPointRepository { get; }
  public IMemoryRepository MemoryRepository { get; }
  public IAlertRepository AlertRepository { get; }
  public IAlertDeliveryRepository AlertDeliveryRepository { get; }

  Task<int> SaveChangesAsync(CancellationToken ct = default);
  Task ExecuteInTransactionAsync(Func<CancellationToken, Task> action, CancellationToken ct = default);
}