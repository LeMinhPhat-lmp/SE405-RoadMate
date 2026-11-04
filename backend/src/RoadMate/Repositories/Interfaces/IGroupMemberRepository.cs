namespace RoadMate.Repositories.Interfaces;

public interface IGroupMemberRepository : IRepository<GroupMember>
{
  Task<GroupMember?> GetAsync(Guid groupId, Guid userId, CancellationToken ct = default);
}
