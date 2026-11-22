using RoadMate.Application.Groups;
using RoadMate.Application.Groups.DTOs.Responses;
using RoadMate.Domain.Groups;
using RoadMate.Domain.ValueObjects;

namespace RoadMate.Application.Abstractions.Persistence;

public interface IGroupReadModel
{
  Task<IReadOnlyList<GroupListItemResponse>> ListForUserAsync(Guid userId, CancellationToken ct = default);
  Task<GroupResponse?> GetAsync(Guid groupId, Guid userId, CancellationToken ct = default);
  Task<GroupSnapshotResponse?> GetSnapshotAsync(Guid groupId, Guid userId, DateTimeOffset? messagesSince, int maxMessages, CancellationToken ct = default);
  Task<IReadOnlyList<GroupMessageResponse>> ListMessagesAsync(Guid groupId, DateTimeOffset? since, int max, CancellationToken ct = default);
}
