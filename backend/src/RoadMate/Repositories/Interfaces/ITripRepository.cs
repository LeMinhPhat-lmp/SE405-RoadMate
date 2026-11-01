namespace RoadMate.Repositories.Interfaces;

public interface ITripRepository : IRepository<Trip>, ISoftDeleteRepository<Trip>
{
  Task<Trip?> GetOwnedAsync(Guid id, Guid userId, CancellationToken ct = default);
  Task<Trip?> GetOwnedWithDetailsAsync(Guid id, Guid userId, CancellationToken ct = default);
  Task<List<Trip>> ListByOwnerAsync(Guid userId, string? status, CancellationToken ct = default);
  Task<bool> HasActiveAsync(Guid userId, CancellationToken ct = default);
  /// <summary>Chuyến theo link theo dõi công khai; null nếu token sai định dạng, không tồn tại hoặc đã hết hạn (kèm Owner + Live).</summary>
  Task<Trip?> GetByActiveTrackingTokenAsync(string token, DateTime now, CancellationToken ct = default);
}
