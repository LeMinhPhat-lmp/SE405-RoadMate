namespace RoadMate.Repositories.Interfaces;

public interface IEmergencyContactRepository : IRepository<EmergencyContact>, ISoftDeleteRepository<EmergencyContact>
{
  Task<List<EmergencyContact>> ListByUserAsync(Guid userId, CancellationToken ct = default);
  Task<int> CountByUserAsync(Guid userId, CancellationToken ct = default);
  Task<EmergencyContact?> GetOwnedAsync(Guid id, Guid userId, CancellationToken ct = default);
  Task<List<EmergencyContact>> GetByIdsAsync(IEnumerable<Guid> ids, Guid userId, CancellationToken ct = default);
  Task<List<EmergencyContact>> ListByPhoneAsync(string phone, CancellationToken ct = default);
  /// <summary>Người nhận cảnh báo của chuyến; nếu chuyến chưa chọn (hoặc người đã chọn đều bị xoá) thì lấy toàn bộ liên hệ của user.</summary>
  Task<List<EmergencyContact>> GetAlertRecipientsAsync(Guid tripId, Guid userId, CancellationToken ct = default);
}
