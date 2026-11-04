namespace RoadMate.Repositories.Interfaces;

public interface IAlertRepository : IRepository<Alert>
{
  Task<Alert?> GetByClientIdAsync(string clientId, CancellationToken ct = default);
  Task<Alert?> GetOwnedAsync(Guid id, Guid userId, CancellationToken ct = default);
  Task<List<Alert>> ListByTripAsync(Guid tripId, CancellationToken ct = default);
  Task<List<Alert>> ListOpenByTripAsync(Guid tripId, CancellationToken ct = default);
  /// <summary>Cảnh báo đang mở của chuyến theo loại (tracked).</summary>
  Task<List<Alert>> ListOpenByTypeAsync(Guid tripId, string type, CancellationToken ct = default);
  Task<bool> HasOpenAsync(Guid tripId, string type, CancellationToken ct = default);
  /// <summary>Đã có cảnh báo loại này của chuyến được tạo từ <paramref name="since"/> trở đi (kể cả đã đóng)?</summary>
  Task<bool> ExistsSinceAsync(Guid tripId, string type, DateTime since, CancellationToken ct = default);
  Task<int> CountByTripAsync(Guid tripId, CancellationToken ct = default);
  Task<List<Alert>> ListDueForRepeatAsync(DateTime now, int take, CancellationToken ct = default);
}
