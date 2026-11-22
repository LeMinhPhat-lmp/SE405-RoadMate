using RoadMate.Application.Groups.DTOs.Requests;
using RoadMate.Application.Groups.DTOs.Responses;
using RoadMate.Domain.Common;

namespace RoadMate.Application.Groups.Interfaces;

public interface IGroupService
{
  Task<Result<GroupResponse>> CreateAsync(Guid userId, CreateGroupRequest req, CancellationToken ct);
  /// <summary>By invite code. Idempotent when already a member.</summary>
  Task<Result<GroupResponse>> JoinAsync(Guid userId, JoinGroupRequest req, CancellationToken ct);
  Task<Result<List<GroupListItemResponse>>> ListMineAsync(Guid userId, CancellationToken ct);
  Task<Result<GroupResponse>> GetAsync(Guid userId, Guid groupId, CancellationToken ct);
  /// <summary>Leader leaving hands over to the earliest-joined member; the last member leaving closes the group.</summary>
  Task<Result> LeaveAsync(Guid userId, Guid groupId, CancellationToken ct);
  Task<Result> RemoveMemberAsync(Guid userId, Guid groupId, Guid memberUserId, CancellationToken ct);
  Task<Result<GroupResponse>> SetMeetingPointAsync(Guid userId, Guid groupId, MeetingPointRequest req, CancellationToken ct);
  /// <summary>open → riding (enables straggler alerts).</summary>
  Task<Result<GroupResponse>> StartAsync(Guid userId, Guid groupId, CancellationToken ct);
  Task<Result> CloseAsync(Guid userId, Guid groupId, CancellationToken ct);
  Task<Result<GroupResponse>> RegenerateCodeAsync(Guid userId, Guid groupId, CancellationToken ct);
  /// <summary>Attach my trip to a group (or detach with null). Needed for heartbeats to update the group map.</summary>
  Task<Result> AttachTripAsync(Guid userId, Guid tripId, Guid? groupId, CancellationToken ct);
}
