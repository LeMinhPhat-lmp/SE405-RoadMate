namespace RoadMate.Repositories.Interfaces;

public interface ITripRepository : IRepository<Trip>, ISoftDeleteRepository<Trip>
{
  Task<Trip?> GetOwnedAsync(Guid id, Guid userId, CancellationToken ct = default);
  Task<Trip?> GetOwnedWithDetailsAsync(Guid id, Guid userId, CancellationToken ct = default);
  Task<List<Trip>> ListByOwnerAsync(Guid userId, string? status, CancellationToken ct = default);
  Task<bool> HasActiveAsync(Guid userId, CancellationToken ct = default);
  /// <summary>Id mọi chuyến đang chạy (cho job giám sát phía server).</summary>
  Task<List<Guid>> ListActiveIdsAsync(CancellationToken ct = default);
  /// <summary>Chuyến của <paramref name="ownerId"/> đang gắn với nhóm (tracked), để gỡ khi rời/bị xoá khỏi nhóm.</summary>
  Task<List<Trip>> ListByGroupAndOwnerAsync(Guid groupId, Guid ownerId, CancellationToken ct = default);
  /// <summary>Chuyến đang chạy kèm Owner + Live (tracked) cho job giám sát; null nếu không còn active.</summary>
  Task<Trip?> GetActiveForMonitoringAsync(Guid id, CancellationToken ct = default);
  /// <summary>Chuyến theo link theo dõi công khai; null nếu token sai định dạng, không tồn tại hoặc đã hết hạn (kèm Owner + Live).</summary>
  Task<Trip?> GetByActiveTrackingTokenAsync(string token, DateTime now, CancellationToken ct = default);
}
