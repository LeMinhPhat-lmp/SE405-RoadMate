using RoadMate.Data;
using RoadMate.Infrastructure.Notifications;

namespace RoadMate.Services.Groups;

public class GroupService(IUnitOfWork uow, IPushSender push, IOptions<GroupOptions> options, TimeProvider clock,
  ILogger<GroupService> log) : IGroupService
{
  private const int MaxCodeAttempts = 5;
  private GroupOptions Opt => options.Value;

  // ------------------------------------------------------------------ tạo / vào / rời

  public async Task<Result<GroupDto>> CreateAsync(Guid userId, CreateGroupRequest req, CancellationToken ct)
  {
    var now = clock.UtcNow();
    var valid = GroupRequestValidator.ValidateCreate(req, now);
    if (valid.IsFailure) return valid.Error;
    if (await uow.UserRepository.GetByIdAsync(userId, false, ct) is null) return Errors.UserNotSynced;

    for (var attempt = 1; attempt <= MaxCodeAttempts; attempt++)
    {
      var group = new Group
      {
        Id = Guid.NewGuid(), Code = GroupCode.Generate(), LeaderId = userId, Name = req.Name.Trim(), Status = GroupStatus.Open,
        MeetingName = req.MeetingName?.Trim(), MeetingLat = req.MeetingLat, MeetingLng = req.MeetingLng, DepartAt = req.DepartAt?.ToUtc()
      };
      await uow.GroupRepository.AddAsync(group, ct);
      await uow.GroupMemberRepository.AddAsync(new GroupMember { Id = Guid.NewGuid(), GroupId = group.Id, UserId = userId, Role = GroupRole.Leader, JoinedAt = now }, ct);
      try
      {
        await uow.SaveChangesAsync(ct);
      }
      catch (DbUpdateException ex) when (ex.IsUniqueViolation())
      {
        log.LogWarning("Mã nhóm {Code} bị trùng, thử mã khác (lần {Attempt})", group.Code, attempt);
        uow.DiscardChanges();
        continue;
      }
      log.LogInformation("Nhóm {GroupId} được tạo bởi {UserId}", group.Id, userId);
      return await ReloadAsync(group.Id, ct);
    }
    return Error.Failure("CODE_GENERATION_FAILED", "Không tạo được mã nhóm, hãy thử lại.");
  }

  public async Task<Result<GroupDto>> JoinAsync(Guid userId, JoinGroupRequest req, CancellationToken ct)
  {
    var code = GroupCode.Normalize(req.Code);
    if (code is null) return GroupErrors.InvalidCode;
    var group = await uow.GroupRepository.GetByCodeAsync(code, ct);
    if (group is null) return GroupErrors.CodeNotFound;
    if (group.Members.Any(m => m.UserId == userId)) return group.ToDto(Opt.MaxMembers);   // đã ở trong nhóm
    if (group.Status == GroupStatus.Closed) return GroupErrors.Closed;
    if (group.Members.Count >= Opt.MaxMembers) return Error.Conflict("GROUP_FULL", $"Nhóm đã đủ {Opt.MaxMembers} thành viên.");
    if (await uow.UserRepository.GetByIdAsync(userId, false, ct) is null) return Errors.UserNotSynced;

    await uow.GroupMemberRepository.AddAsync(new GroupMember { Id = Guid.NewGuid(), GroupId = group.Id, UserId = userId, Role = GroupRole.Member, JoinedAt = clock.UtcNow() }, ct);
    try
    {
      await uow.SaveChangesAsync(ct);
    }
    catch (DbUpdateException ex) when (ex.IsUniqueViolation())
    {
      uow.DiscardChanges();                       // hai request vào nhóm đồng thời: coi như đã vào
    }
    log.LogInformation("User {UserId} vào nhóm {GroupId}", userId, group.Id);
    return await ReloadAsync(group.Id, ct);
  }

  public async Task<Result> LeaveAsync(Guid userId, Guid groupId, CancellationToken ct)
  {
    var loaded = await LoadForMemberAsync(userId, groupId, ct);
    if (loaded.IsFailure) return loaded.Error;
    var (group, member) = loaded.Value;
    await RemoveMemberCoreAsync(group, member, ct);
    return Result.Success();
  }

  public async Task<Result> RemoveMemberAsync(Guid userId, Guid groupId, Guid memberUserId, CancellationToken ct)
  {
    var loaded = await LoadForLeaderAsync(userId, groupId, ct);
    if (loaded.IsFailure) return loaded.Error;
    if (memberUserId == userId) return Error.Validation("CANNOT_REMOVE_SELF", "Muốn rời nhóm hãy dùng chức năng rời nhóm.");
    var target = loaded.Value.Group.Members.FirstOrDefault(m => m.UserId == memberUserId);
    if (target is null) return GroupErrors.MemberNotFound;
    await RemoveMemberCoreAsync(loaded.Value.Group, target, ct);
    return Result.Success();
  }

  /// <summary>Xoá thành viên, chuyển quyền trưởng đoàn nếu cần, đóng nhóm nếu hết người, gỡ chuyến của người đó khỏi nhóm.</summary>
  private async Task RemoveMemberCoreAsync(Group group, GroupMember member, CancellationToken ct)
  {
    uow.GroupMemberRepository.Remove(member);
    var rest = group.Members.Where(m => m.UserId != member.UserId).OrderBy(m => m.JoinedAt).ToList();
    if (rest.Count == 0)
    {
      group.Status = GroupStatus.Closed;
      log.LogInformation("Nhóm {GroupId} đóng vì không còn thành viên", group.Id);
    }
    else if (member.UserId == group.LeaderId)
    {
      group.LeaderId = rest[0].UserId;
      rest[0].Role = GroupRole.Leader;
      log.LogInformation("Nhóm {GroupId}: chuyển quyền trưởng đoàn từ {Old} sang {New}", group.Id, member.UserId, rest[0].UserId);
    }
    foreach (var trip in await uow.TripRepository.ListByGroupAndOwnerAsync(group.Id, member.UserId, ct)) trip.GroupId = null;
    await uow.SaveChangesAsync(ct);
    log.LogInformation("User {UserId} không còn trong nhóm {GroupId}", member.UserId, group.Id);
  }

  // ------------------------------------------------------------------ xem

  public async Task<Result<List<GroupListItemDto>>> ListMineAsync(Guid userId, CancellationToken ct) =>
    (await uow.GroupRepository.ListByMemberAsync(userId, ct))
      .Select(g => new GroupListItemDto(g.Id, g.Name, g.Status, g.Members.First(m => m.UserId == userId).Role, g.Members.Count, g.DepartAt))
      .ToList();

  public async Task<Result<GroupDto>> GetAsync(Guid userId, Guid groupId, CancellationToken ct)
  {
    var loaded = await LoadForMemberAsync(userId, groupId, ct);
    return loaded.IsFailure ? loaded.Error : loaded.Value.Group.ToDto(Opt.MaxMembers);
  }

  // ------------------------------------------------------------------ trưởng đoàn

  public async Task<Result<GroupDto>> SetMeetingPointAsync(Guid userId, Guid groupId, MeetingPointRequest req, CancellationToken ct)
  {
    var loaded = await LoadForLeaderAsync(userId, groupId, ct);
    if (loaded.IsFailure) return loaded.Error;
    var group = loaded.Value.Group;
    if (group.Status == GroupStatus.Closed) return GroupErrors.Closed;
    var valid = GroupRequestValidator.ValidateMeeting(req.Lat, req.Lng, req.DepartAt, clock.UtcNow());
    if (valid.IsFailure) return valid.Error;

    group.MeetingName = req.Name?.Trim();
    group.MeetingLat = req.Lat;
    group.MeetingLng = req.Lng;
    group.DepartAt = req.DepartAt?.ToUtc();
    await uow.SaveChangesAsync(ct);
    return group.ToDto(Opt.MaxMembers);
  }

  public async Task<Result<GroupDto>> StartAsync(Guid userId, Guid groupId, CancellationToken ct)
  {
    var loaded = await LoadForLeaderAsync(userId, groupId, ct);
    if (loaded.IsFailure) return loaded.Error;
    var group = loaded.Value.Group;
    if (group.Status == GroupStatus.Closed) return GroupErrors.Closed;
    if (group.Status != GroupStatus.Riding)
    {
      group.Status = GroupStatus.Riding;
      await uow.SaveChangesAsync(ct);
      log.LogInformation("Nhóm {GroupId} xuất phát với {Count} thành viên", group.Id, group.Members.Count);
    }
    return group.ToDto(Opt.MaxMembers);
  }

  public async Task<Result> CloseAsync(Guid userId, Guid groupId, CancellationToken ct)
  {
    var loaded = await LoadForLeaderAsync(userId, groupId, ct);
    if (loaded.IsFailure) return loaded.Error;
    var group = loaded.Value.Group;
    if (group.Status != GroupStatus.Closed)
    {
      group.Status = GroupStatus.Closed;
      await uow.SaveChangesAsync(ct);
      log.LogInformation("Nhóm {GroupId} được trưởng đoàn đóng", group.Id);
    }
    return Result.Success();
  }

  public async Task<Result<GroupDto>> RegenerateCodeAsync(Guid userId, Guid groupId, CancellationToken ct)
  {
    for (var attempt = 1; attempt <= MaxCodeAttempts; attempt++)
    {
      var loaded = await LoadForLeaderAsync(userId, groupId, ct);
      if (loaded.IsFailure) return loaded.Error;
      if (loaded.Value.Group.Status == GroupStatus.Closed) return GroupErrors.Closed;

      loaded.Value.Group.Code = GroupCode.Generate();
      try
      {
        await uow.SaveChangesAsync(ct);
        log.LogInformation("Nhóm {GroupId} đổi mã mời", groupId);
        return loaded.Value.Group.ToDto(Opt.MaxMembers);
      }
      catch (DbUpdateException ex) when (ex.IsUniqueViolation())
      {
        uow.DiscardChanges();                     // trùng mã: tải lại nhóm rồi thử mã khác
      }
    }
    return Error.Failure("CODE_GENERATION_FAILED", "Không tạo được mã nhóm, hãy thử lại.");
  }

  // ------------------------------------------------------------------ chuyến ↔ nhóm

  public async Task<Result> AttachTripAsync(Guid userId, Guid tripId, Guid? groupId, CancellationToken ct)
  {
    var trip = await uow.TripRepository.GetOwnedAsync(tripId, userId, ct);
    if (trip is null) return Errors.TripNotFound;
    if (trip.Status == TripStatus.Finished) return Errors.TripFinished;

    if (groupId is null) trip.GroupId = null;
    else
    {
      var loaded = await LoadForMemberAsync(userId, groupId.Value, ct);
      if (loaded.IsFailure) return loaded.Error;
      if (loaded.Value.Group.Status == GroupStatus.Closed) return GroupErrors.Closed;
      trip.GroupId = groupId;
    }
    await uow.SaveChangesAsync(ct);
    log.LogInformation("Chuyến {TripId} {Action} nhóm {GroupId}", tripId, groupId is null ? "gỡ khỏi" : "gắn vào", groupId);
    return Result.Success();
  }

  // ------------------------------------------------------------------ tin nhắn

  public async Task<Result<GroupMessageDto>> SendMessageAsync(Guid userId, Guid groupId, SendGroupMessageRequest req, CancellationToken ct)
  {
    if (!QuickMessages.IsValid(req.QuickType))
      return Error.Validation("INVALID_QUICK_TYPE", $"quickType phải là một trong: {string.Join(", ", QuickMessages.All)}.");
    var loaded = await LoadForMemberAsync(userId, groupId, ct);
    if (loaded.IsFailure) return loaded.Error;
    var (group, sender) = loaded.Value;
    if (group.Status == GroupStatus.Closed) return GroupErrors.Closed;

    var message = new GroupMessage { Id = Guid.NewGuid(), GroupId = groupId, SenderId = userId, QuickType = req.QuickType, Text = QuickMessages.Label(req.QuickType) };
    await uow.GroupMessageRepository.AddAsync(message, ct);
    await uow.SaveChangesAsync(ct);

    var senderName = sender.User?.Name ?? "";
    if (QuickMessages.PushesToGroup(req.QuickType)) await PushToGroupAsync(group, userId, senderName, message, ct);
    return new GroupMessageDto(message.Id, userId, senderName, req.QuickType, message.Text, message.CreatedAt);
  }

  public async Task<Result<List<GroupMessageDto>>> ListMessagesAsync(Guid userId, Guid groupId, DateTime? since, CancellationToken ct)
  {
    var loaded = await LoadForMemberAsync(userId, groupId, ct);
    if (loaded.IsFailure) return loaded.Error;
    return (await uow.GroupMessageRepository.ListAsync(groupId, since, Opt.MaxMessagesPerPoll, ct)).Select(m => m.ToDto()).ToList();
  }

  private async Task PushToGroupAsync(Group group, Guid senderId, string senderName, GroupMessage message, CancellationToken ct)
  {
    var data = new Dictionary<string, string>
    {
      ["type"] = "group_message", ["groupId"] = group.Id.ToString(), ["quickType"] = message.QuickType!, ["senderName"] = senderName
    };
    var sent = 0;
    foreach (var token in group.Members.Where(m => m.UserId != senderId).Select(m => m.User?.FcmToken).Where(t => !string.IsNullOrEmpty(t)))
    {
      var r = await push.SendAsync(token!, $"RoadMate nhóm: {message.Text}", $"{senderName}: {message.Text}", data, ct);
      if (r.IsSuccess) sent++;
    }
    log.LogInformation("Tin '{QuickType}' của nhóm {GroupId}: đã push {Sent} thành viên", message.QuickType, group.Id, sent);
  }

  // ------------------------------------------------------------------ vị trí (gọi từ heartbeat/points)

  public async Task<GroupSnapshotDto?> ReportLocationAsync(Guid userId, Guid groupId, double lat, double lng, int? battery, DateTime? messagesSince, CancellationToken ct)
  {
    try
    {
      var group = await uow.GroupRepository.GetWithMembersAsync(groupId, ct);
      var me = group?.Members.FirstOrDefault(m => m.UserId == userId);
      if (group is null || me is null) return null;                // đã bị xoá khỏi nhóm

      me.LastLat = lat; me.LastLng = lng; me.LastBattery = battery; me.LocationUpdatedAt = clock.UtcNow();
      await uow.SaveChangesAsync(ct);

      var messages = messagesSince is null
        ? new List<GroupMessageDto>()
        : (await uow.GroupMessageRepository.ListAsync(groupId, messagesSince, Opt.MaxMessagesPerPoll, ct)).Select(m => m.ToDto()).ToList();
      return new GroupSnapshotDto(group.Id, group.Status, group.DepartAt, group.ToMemberDtos(), messages);
    }
    catch (Exception ex) when (ex is not OperationCanceledException)
    {
      log.LogError(ex, "Cập nhật vị trí nhóm {GroupId} của user {UserId} lỗi", groupId, userId);
      uow.DiscardChanges();
      return null;
    }
  }

  // ------------------------------------------------------------------ helper

  private async Task<Result<(Group Group, GroupMember Me)>> LoadForMemberAsync(Guid userId, Guid groupId, CancellationToken ct)
  {
    var group = await uow.GroupRepository.GetWithMembersAsync(groupId, ct);
    var me = group?.Members.FirstOrDefault(m => m.UserId == userId);
    if (group is null || me is null) return GroupErrors.NotFound;
    return (group, me);
  }

  private async Task<Result<(Group Group, GroupMember Me)>> LoadForLeaderAsync(Guid userId, Guid groupId, CancellationToken ct)
  {
    var loaded = await LoadForMemberAsync(userId, groupId, ct);
    if (loaded.IsFailure) return loaded.Error;
    return loaded.Value.Group.LeaderId == userId ? loaded : GroupErrors.NotLeader;
  }

  /// <summary>Tải lại từ DB (sạch change tracker) để DTO có đủ tên thành viên sau khi thêm/sửa.</summary>
  private async Task<Result<GroupDto>> ReloadAsync(Guid groupId, CancellationToken ct)
  {
    uow.DiscardChanges();
    var group = await uow.GroupRepository.GetWithMembersAsync(groupId, ct);
    return group is null ? GroupErrors.NotFound : group.ToDto(Opt.MaxMembers);
  }
}
