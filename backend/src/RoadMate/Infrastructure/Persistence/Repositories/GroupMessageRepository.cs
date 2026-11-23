using Microsoft.EntityFrameworkCore;
using RoadMate.Application.Abstractions.Persistence;
using RoadMate.Domain.Groups;

namespace RoadMate.Infrastructure.Persistence.Repositories;

public class GroupMessageRepository(AppDbContext db) : RepositoryBase<GroupMessage>(db), IGroupMessageRepository
{
  public Task<int> PurgeOlderThanAsync(DateTimeOffset cutoff, CancellationToken ct = default) =>
    Set.Where(m => m.CreatedAt < cutoff).ExecuteDeleteAsync(ct);
}
