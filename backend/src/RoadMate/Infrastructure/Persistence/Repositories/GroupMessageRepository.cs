using Microsoft.EntityFrameworkCore;
using RoadMate.Application.Abstractions.Persistence;
using RoadMate.Domain.Groups;

namespace RoadMate.Infrastructure.Persistence.Repositories;

public class GroupMessageRepository(AppDbContext db) : IGroupMessageRepository
{
  public async Task AddAsync(GroupMessage message, CancellationToken ct = default) => await db.GroupMessages.AddAsync(message, ct);

  public Task<int> PurgeOlderThanAsync(DateTimeOffset cutoff, CancellationToken ct = default) =>
    db.GroupMessages.Where(m => m.CreatedAt < cutoff).ExecuteDeleteAsync(ct);
}
