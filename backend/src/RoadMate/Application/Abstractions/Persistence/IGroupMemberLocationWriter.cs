using RoadMate.Application.Groups;
using RoadMate.Domain.Groups;
using RoadMate.Domain.ValueObjects;

namespace RoadMate.Application.Abstractions.Persistence;

/// <summary>Set-based "overwrite my last position" (heartbeat hot path; no aggregate load).</summary>
public interface IGroupMemberLocationWriter
{
  Task UpdateAsync(Guid groupId, Guid userId, GeoPoint at, int? battery, DateTimeOffset now, CancellationToken ct = default);
}
