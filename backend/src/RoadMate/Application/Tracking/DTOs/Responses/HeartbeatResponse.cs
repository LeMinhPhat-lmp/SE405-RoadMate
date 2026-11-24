using RoadMate.Application.Groups;
using RoadMate.Application.Groups.DTOs.Responses;

namespace RoadMate.Application.Tracking.DTOs.Responses;

/// <param name="Group">Member positions + new messages; null when the trip is not attached to a group.</param>
public record HeartbeatResponse(GroupSnapshotResponse? Group);
