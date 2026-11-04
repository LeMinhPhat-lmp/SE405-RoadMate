namespace RoadMate.Repositories.Interfaces;

public interface IUnitOfWork
{
  IUserRepository UserRepository { get; }
  IEmergencyContactRepository EmergencyContactRepository { get; }
  ITripRepository TripRepository { get; }
  ITripPointRepository TripPointRepository { get; }
  IMemoryRepository MemoryRepository { get; }
  IAlertRepository AlertRepository { get; }
  IAlertDeliveryRepository AlertDeliveryRepository { get; }
  IAlertAckRepository AlertAckRepository { get; }
  IGroupRepository GroupRepository { get; }
  IGroupMemberRepository GroupMemberRepository { get; }
  IGroupMessageRepository GroupMessageRepository { get; }

  Task<int> SaveChangesAsync(CancellationToken ct = default);
  /// <summary>Bỏ mọi thay đổi chưa lưu (vd sau khi SaveChanges lỗi) để dùng tiếp cùng scope an toàn.</summary>
  void DiscardChanges();
  Task ExecuteInTransactionAsync(Func<CancellationToken, Task> action, CancellationToken ct = default);
}
