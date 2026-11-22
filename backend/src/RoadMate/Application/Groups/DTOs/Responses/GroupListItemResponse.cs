namespace RoadMate.Application.Groups.DTOs.Responses;

public record GroupListItemResponse(Guid Id, string Name, string Status, string MyRole, int MemberCount, DateTime? DepartAt);
