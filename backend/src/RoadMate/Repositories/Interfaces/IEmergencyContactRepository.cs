namespace RoadMate.Repositories.Interfaces;

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
