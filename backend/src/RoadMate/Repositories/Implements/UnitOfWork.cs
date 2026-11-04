using RoadMate.Data;

namespace RoadMate.Repositories.Implements;

public class UnitOfWork(
  AppDbContext context,
  IUserRepository userRepository,
  IEmergencyContactRepository emergencyContactRepository,
  ITripRepository tripRepository,
  ITripPointRepository tripPointRepository,
  IMemoryRepository memoryRepository,
  IAlertRepository alertRepository,
  IAlertDeliveryRepository alertDeliveryRepository,
  IAlertAckRepository alertAckRepository,
  IGroupRepository groupRepository,
  IGroupMemberRepository groupMemberRepository,
  IGroupMessageRepository groupMessageRepository
) : IUnitOfWork
{
  public IUserRepository UserRepository => userRepository;
  public IEmergencyContactRepository EmergencyContactRepository => emergencyContactRepository;
  public ITripRepository TripRepository => tripRepository;
  public ITripPointRepository TripPointRepository => tripPointRepository;
  public IMemoryRepository MemoryRepository => memoryRepository;
  public IAlertRepository AlertRepository => alertRepository;
  public IAlertDeliveryRepository AlertDeliveryRepository => alertDeliveryRepository;
  public IAlertAckRepository AlertAckRepository => alertAckRepository;
  public IGroupRepository GroupRepository => groupRepository;
  public IGroupMemberRepository GroupMemberRepository => groupMemberRepository;
  public IGroupMessageRepository GroupMessageRepository => groupMessageRepository;

  public Task<int> SaveChangesAsync(CancellationToken ct = default) => context.SaveChangesAsync(ct);

  public void DiscardChanges() => context.ChangeTracker.Clear();

  public async Task ExecuteInTransactionAsync(Func<CancellationToken, Task> action, CancellationToken ct = default)
  {
    await using var tx = await context.Database.BeginTransactionAsync(ct);
    try { await action(ct); await tx.CommitAsync(ct); }
    catch { await tx.RollbackAsync(ct); throw; }
  }
}
