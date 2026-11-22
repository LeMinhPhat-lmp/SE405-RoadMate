using RoadMate.Application.Groups;
using RoadMate.Domain.Groups;
using RoadMate.Domain.ValueObjects;

namespace RoadMate.Application.Abstractions.Persistence;

public interface IGroupRepository : IRepository<Group>, ISoftDeleteRepository<Group>
{
  /// <summary>Tracked, with members.</summary>
  Task<Group?> GetWithMembersAsync(Guid id, CancellationToken ct = default);
  Task<Group?> GetByCodeAsync(string code, CancellationToken ct = default);
  Task<bool> CodeExistsAsync(string code, CancellationToken ct = default);
  /// <summary>Tracked groups in status Riding with members (straggler monitor).</summary>
  Task<IReadOnlyList<Group>> ListRidingAsync(CancellationToken ct = default);
}
