namespace RoadMate.Application.Groups.DTOs.Requests;

public record CreateGroupRequest(string Name, string? MeetingName, double? MeetingLat, double? MeetingLng, DateTime? DepartAt);
