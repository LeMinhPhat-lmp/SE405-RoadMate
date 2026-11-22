namespace RoadMate.Application.Groups.DTOs.Responses;

/// <summary>Ảnh chụp nhóm trả kèm heartbeat: vị trí mọi thành viên + tin nhắn mới (nếu client gửi messagesSince).</summary>
public record GroupSnapshotResponse(Guid GroupId, string Status, DateTime? DepartAt, List<GroupMemberResponse> Members, List<GroupMessageResponse> NewMessages);
