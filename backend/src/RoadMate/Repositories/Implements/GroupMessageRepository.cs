using RoadMate.Data;

namespace RoadMate.Repositories.Implements;

public class GroupMessageRepository(AppDbContext context) : Repository<GroupMessage>(context), IGroupMessageRepository
{
  public async Task<List<GroupMessage>> ListAsync(Guid groupId, DateTime? since, int take, CancellationToken ct = default)
  {
    var query = Set.AsNoTracking().Include(m => m.Sender).Where(m => m.GroupId == groupId);
    if (since is { } s)
    {
      var from = new DateTimeOffset(s.ToUtc());
      query = query.Where(m => m.CreatedAt > from);
    }
    var latest = await query.OrderByDescending(m => m.CreatedAt).Take(take).ToListAsync(ct);
    latest.Reverse();
    return latest;
  }

  public Task<int> DeleteOlderThanAsync(DateTime cutoff, CancellationToken ct = default)
  {
    var before = new DateTimeOffset(cutoff.ToUtc());
    return Set.Where(m => m.CreatedAt < before).ExecuteDeleteAsync(ct);
  }
}
