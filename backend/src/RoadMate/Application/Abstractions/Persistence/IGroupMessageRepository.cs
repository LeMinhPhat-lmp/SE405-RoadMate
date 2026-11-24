using RoadMate.Domain.Groups;

namespace RoadMate.Application.Abstractions.Persistence;

public interface IGroupMessageRepository : IGenericRepository<GroupMessage>
{
  Task<int> PurgeOlderThanAsync(DateTimeOffset cutoff, CancellationToken ct = default);
}
