namespace RoadMate.Dtos.Groups;

public record CreateGroupRequest(string Name, string? MeetingName, double? MeetingLat, double? MeetingLng, DateTime? DepartAt);
public record JoinGroupRequest(string Code);
public record MeetingPointRequest(string? Name, double Lat, double Lng, DateTime? DepartAt);
/// <param name="QuickType">rest | out_of_fuel | need_help | police.</param>
public record SendGroupMessageRequest(string QuickType);
/// <param name="GroupId">null = gỡ chuyến khỏi nhóm.</param>
public record AttachTripRequest(Guid? GroupId);

/// <param name="DistanceToLeaderMeters">Khoảng cách tới trưởng đoàn theo vị trí mới nhất của cả hai; null nếu thiếu vị trí hoặc là trưởng đoàn.</param>
public record GroupMemberDto(Guid UserId, string Name, string Role, double? Lat, double? Lng, int? Battery, DateTime? LocationUpdatedAt, int? DistanceToLeaderMeters);
public record MeetingPointDto(string? Name, double Lat, double Lng);
/// <param name="Code">Mã mời 6 ký tự (chỉ thành viên thấy).</param>
public record GroupDto(Guid Id, string Name, string Code, string Status, Guid LeaderId, MeetingPointDto? Meeting, DateTime? DepartAt,
  int MaxMembers, List<GroupMemberDto> Members);
public record GroupListItemDto(Guid Id, string Name, string Status, string MyRole, int MemberCount, DateTime? DepartAt);
public record GroupMessageDto(Guid Id, Guid SenderId, string SenderName, string QuickType, string Label, DateTimeOffset SentAt);

/// <summary>Ảnh chụp nhóm trả kèm heartbeat: vị trí mọi thành viên + tin nhắn mới (nếu client gửi messagesSince).</summary>
public record GroupSnapshotDto(Guid GroupId, string Status, DateTime? DepartAt, List<GroupMemberDto> Members, List<GroupMessageDto> NewMessages);
