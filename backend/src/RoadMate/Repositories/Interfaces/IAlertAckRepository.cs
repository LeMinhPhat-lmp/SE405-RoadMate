namespace RoadMate.Repositories.Interfaces;

public interface IAlertAckRepository : IRepository<AlertAck>
{
  /// <summary>Các token xác nhận của một cảnh báo (tracked).</summary>
  Task<List<AlertAck>> ListByAlertAsync(Guid alertId, CancellationToken ct = default);
  /// <summary>Theo token công khai (kèm Alert, Alert.User, Alert.Trip); null nếu token sai định dạng/không tồn tại.</summary>
  Task<AlertAck?> GetByTokenAsync(string token, CancellationToken ct = default);
}
