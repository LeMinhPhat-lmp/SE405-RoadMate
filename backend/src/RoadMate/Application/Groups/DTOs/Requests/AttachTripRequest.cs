namespace RoadMate.Application.Groups.DTOs.Requests;

/// <param name="GroupId">null = gỡ chuyến khỏi nhóm.</param>
public record AttachTripRequest(Guid? GroupId);
