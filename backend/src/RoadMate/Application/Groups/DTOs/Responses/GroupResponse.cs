namespace RoadMate.Application.Groups.DTOs.Responses;

/// <param name="Code">Mã mời 6 ký tự (chỉ thành viên thấy).</param>
public record GroupResponse(Guid Id, string Name, string Code, string Status, Guid LeaderId, MeetingPointResponse? Meeting, DateTime? DepartAt,
  int MaxMembers, List<GroupMemberResponse> Members);
