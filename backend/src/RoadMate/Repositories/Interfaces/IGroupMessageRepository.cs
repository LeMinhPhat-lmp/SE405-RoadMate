namespace RoadMate.Repositories.Interfaces;

public interface IGroupMessageRepository : IRepository<GroupMessage>
{
  /// <summary>Tối đa <paramref name="take"/> tin MỚI NHẤT (sau <paramref name="since"/> nếu có), trả theo thứ tự thời gian tăng dần, kèm người gửi.</summary>
  Task<List<GroupMessage>> ListAsync(Guid groupId, DateTime? since, int take, CancellationToken ct = default);
  /// <summary>Xoá hẳn tin cũ hơn <paramref name="cutoff"/> (một câu lệnh SQL, không qua change tracker); trả số tin đã xoá.</summary>
  Task<int> DeleteOlderThanAsync(DateTime cutoff, CancellationToken ct = default);
}
