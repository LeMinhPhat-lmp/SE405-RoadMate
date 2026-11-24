namespace RoadMate.Application.Groups.DTOs.Responses;

/// <param name="DistanceToLeaderMeters">Khoảng cách tới trưởng đoàn theo vị trí mới nhất của cả hai; null nếu thiếu vị trí hoặc là trưởng đoàn.</param>
public record GroupMemberResponse(Guid UserId, string Name, string Role, double? Lat, double? Lng, int? Battery, DateTime? LocationUpdatedAt, int? DistanceToLeaderMeters);
