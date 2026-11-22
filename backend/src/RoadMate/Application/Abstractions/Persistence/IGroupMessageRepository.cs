using RoadMate.Application.Groups;
using RoadMate.Domain.Groups;
using RoadMate.Domain.ValueObjects;

namespace RoadMate.Application.Abstractions.Persistence;

public interface IGroupMessageRepository
{
  Task AddAsync(GroupMessage message, CancellationToken ct = default);
  Task<int> PurgeOlderThanAsync(DateTimeOffset cutoff, CancellationToken ct = default);
}
