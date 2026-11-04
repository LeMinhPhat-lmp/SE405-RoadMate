using RoadMate.Data;

namespace RoadMate.Repositories.Implements;

public class GroupMemberRepository(AppDbContext context) : Repository<GroupMember>(context), IGroupMemberRepository
{
  public Task<GroupMember?> GetAsync(Guid groupId, Guid userId, CancellationToken ct = default) =>
    Set.FirstOrDefaultAsync(m => m.GroupId == groupId && m.UserId == userId, ct);
}
