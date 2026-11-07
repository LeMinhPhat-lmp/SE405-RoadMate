namespace RoadMate.Repositories.Interfaces;

public interface IGroupRepository : IRepository<Group>, ISoftDeleteRepository<Group>
{
  /// <summary>Mã đã được nhóm nào (kể cả đã đóng) dùng chưa.</summary>
  Task<bool> CodeExistsAsync(string code, CancellationToken ct = default);
  /// <summary>Theo mã mời, kèm thành viên + User (tracked).</summary>
  Task<Group?> GetByCodeAsync(string code, CancellationToken ct = default);
  /// <summary>Theo id, kèm thành viên + User (tracked).</summary>
  Task<Group?> GetWithMembersAsync(Guid id, CancellationToken ct = default);
  /// <summary>Các nhóm user tham gia (kể cả đã đóng), kèm thành viên (no-tracking), mới nhất trước.</summary>
  Task<List<Group>> ListByMemberAsync(Guid userId, CancellationToken ct = default);
  /// <summary>Các nhóm đang chạy (riding), kèm thành viên + User (no-tracking) cho job giám sát.</summary>
  Task<List<Group>> ListRidingAsync(CancellationToken ct = default);
}
