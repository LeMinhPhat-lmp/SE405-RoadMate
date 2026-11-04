namespace RoadMate.Services.Groups;

public interface IGroupService
{
  Task<Result<GroupDto>> CreateAsync(Guid userId, CreateGroupRequest req, CancellationToken ct);
  /// <summary>Tham gia bằng mã mời. Idempotent nếu đã là thành viên.</summary>
  Task<Result<GroupDto>> JoinAsync(Guid userId, JoinGroupRequest req, CancellationToken ct);
  Task<Result<List<GroupListItemDto>>> ListMineAsync(Guid userId, CancellationToken ct);
  Task<Result<GroupDto>> GetAsync(Guid userId, Guid groupId, CancellationToken ct);
  /// <summary>Rời nhóm. Trưởng đoàn rời thì quyền chuyển cho người vào nhóm sớm nhất; người cuối rời thì nhóm đóng.</summary>
  Task<Result> LeaveAsync(Guid userId, Guid groupId, CancellationToken ct);
  Task<Result> RemoveMemberAsync(Guid userId, Guid groupId, Guid memberUserId, CancellationToken ct);
  Task<Result<GroupDto>> SetMeetingPointAsync(Guid userId, Guid groupId, MeetingPointRequest req, CancellationToken ct);
  /// <summary>open -> riding (bật cảnh báo tụt đoàn).</summary>
  Task<Result<GroupDto>> StartAsync(Guid userId, Guid groupId, CancellationToken ct);
  Task<Result> CloseAsync(Guid userId, Guid groupId, CancellationToken ct);
  /// <summary>Sinh mã mời mới (mã cũ hết hiệu lực), vd khi mã bị lộ.</summary>
  Task<Result<GroupDto>> RegenerateCodeAsync(Guid userId, Guid groupId, CancellationToken ct);
  /// <summary>Gắn chuyến của mình vào nhóm (hoặc gỡ nếu groupId = null). Cần gắn thì heartbeat mới cập nhật vị trí lên nhóm.</summary>
  Task<Result> AttachTripAsync(Guid userId, Guid tripId, Guid? groupId, CancellationToken ct);
  Task<Result<GroupMessageDto>> SendMessageAsync(Guid userId, Guid groupId, SendGroupMessageRequest req, CancellationToken ct);
  Task<Result<List<GroupMessageDto>>> ListMessagesAsync(Guid userId, Guid groupId, DateTime? since, CancellationToken ct);

  /// <summary>
  /// Cập nhật vị trí của <paramref name="userId"/> trong nhóm và trả ảnh chụp nhóm (null nếu không còn là thành viên hoặc có lỗi).
  /// Không bao giờ ném lỗi nghiệp vụ: lỗi ở nhóm không được làm hỏng heartbeat của chuyến.
  /// </summary>
  Task<GroupSnapshotDto?> ReportLocationAsync(Guid userId, Guid groupId, double lat, double lng, int? battery, DateTime? messagesSince, CancellationToken ct);
}
