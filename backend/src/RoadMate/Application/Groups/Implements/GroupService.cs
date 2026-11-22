using Microsoft.Extensions.Options;
using RoadMate.Application.Abstractions.Persistence;
using RoadMate.Application.Common;
using RoadMate.Application.Groups.DTOs.Requests;
using RoadMate.Application.Groups.DTOs.Responses;
using RoadMate.Application.Groups.Interfaces;
using RoadMate.Domain.Common;
using RoadMate.Domain.Groups;

namespace RoadMate.Application.Groups.Implements;

/// <summary>Orchestration only; membership/leadership/limits are rules of the <c>Group</c> aggregate.</summary>
public class GroupService(IGroupRepository groups, IUserRepository users, ITripRepository trips, IUnitOfWork uow, IGroupReadModel read,
  IOptions<GroupOptions> options, TimeProvider clock, ILogger<GroupService> log) : IGroupService
{
  private const int MaxCodeAttempts = 5;
  private int Max => options.Value.MaxMembers;

  public async Task<Result<GroupResponse>> CreateAsync(Guid userId, CreateGroupRequest req, CancellationToken ct)
  {
    if (await users.GetByIdAsync(userId, ct) is null) return Errors.UserNotSynced;
    var now = clock.GetUtcNow();
    for (var attempt = 1; attempt <= MaxCodeAttempts; attempt++)
    {
      var created = Group.Create(userId, req.Name, req.MeetingName, req.MeetingLat, req.MeetingLng, req.DepartAt.ToUtcOffset(), GroupCode.Generate(), now);
      if (created.IsFailure) return created.Error;
      await groups.AddAsync(created.Value, ct);
      try { await uow.SaveChangesAsync(ct); }
      catch (UniqueViolationException)
      {
        log.LogWarning("Mã nhóm {Code} bị trùng, thử mã khác (lần {Attempt})", created.Value.Code, attempt);
        uow.DiscardChanges();
        continue;
      }
      log.LogInformation("Nhóm {GroupId} được tạo bởi {UserId}", created.Value.Id, userId);
      return await DtoAsync(created.Value.Id, userId, ct);
    }
    return Error.Failure("CODE_GENERATION_FAILED", "Không tạo được mã nhóm, hãy thử lại.");
  }

  public async Task<Result<GroupResponse>> JoinAsync(Guid userId, JoinGroupRequest req, CancellationToken ct)
  {
    var code = GroupCode.Normalize(req.Code);
    if (code is null) return GroupErrors.InvalidCode;
    var group = await groups.GetByCodeAsync(code, ct);
    if (group is null) return GroupErrors.CodeNotFound;
    if (!group.IsMember(userId) && await users.GetByIdAsync(userId, ct) is null) return Errors.UserNotSynced;
    var joined = group.Join(userId, clock.GetUtcNow(), Max);
    if (joined.IsFailure) return joined.Error;
    try { await uow.SaveChangesAsync(ct); }
    catch (UniqueViolationException) { uow.DiscardChanges(); }      // two concurrent joins: already in
    log.LogInformation("User {UserId} vào nhóm {GroupId}", userId, group.Id);
    return await DtoAsync(group.Id, userId, ct);
  }

  public Task<Result> LeaveAsync(Guid userId, Guid groupId, CancellationToken ct) =>
    MutateAsync(userId, groupId, leaderOnly: false, g => g.Leave(userId, clock.GetUtcNow()), ct);

  public Task<Result> RemoveMemberAsync(Guid userId, Guid groupId, Guid memberUserId, CancellationToken ct) =>
    MutateAsync(userId, groupId, leaderOnly: true, g => g.RemoveMember(userId, memberUserId, clock.GetUtcNow()), ct);

  public async Task<Result<List<GroupListItemResponse>>> ListMineAsync(Guid userId, CancellationToken ct) => (await read.ListForUserAsync(userId, ct)).ToList();

  public async Task<Result<GroupResponse>> GetAsync(Guid userId, Guid groupId, CancellationToken ct) => await DtoAsync(groupId, userId, ct);

  public Task<Result<GroupResponse>> SetMeetingPointAsync(Guid userId, Guid groupId, MeetingPointRequest req, CancellationToken ct) =>
    MutateWithDtoAsync(userId, groupId, g => g.SetMeetingPoint(userId, req.Name, req.Lat, req.Lng, req.DepartAt.ToUtcOffset(), clock.GetUtcNow()), ct);

  public Task<Result<GroupResponse>> StartAsync(Guid userId, Guid groupId, CancellationToken ct) =>
    MutateWithDtoAsync(userId, groupId, g => g.Start(userId, clock.GetUtcNow()), ct);

  public Task<Result> CloseAsync(Guid userId, Guid groupId, CancellationToken ct) =>
    MutateAsync(userId, groupId, leaderOnly: true, g => g.Close(userId, clock.GetUtcNow()), ct);

  public async Task<Result<GroupResponse>> RegenerateCodeAsync(Guid userId, Guid groupId, CancellationToken ct)
  {
    for (var attempt = 1; attempt <= MaxCodeAttempts; attempt++)
    {
      var group = await groups.GetWithMembersAsync(groupId, ct);
      if (group is null || !group.IsMember(userId)) return GroupErrors.NotFound;
      var r = group.RegenerateCode(userId, GroupCode.Generate(), clock.GetUtcNow());
      if (r.IsFailure) return r.Error;
      try { await uow.SaveChangesAsync(ct); return await DtoAsync(groupId, userId, ct); }
      catch (UniqueViolationException) { uow.DiscardChanges(); }
    }
    return Error.Failure("CODE_GENERATION_FAILED", "Không tạo được mã nhóm, hãy thử lại.");
  }

  public async Task<Result> AttachTripAsync(Guid userId, Guid tripId, Guid? groupId, CancellationToken ct)
  {
    var trip = await trips.GetOwnedAsync(tripId, userId, ct);
    if (trip is null) return Errors.TripNotFound;
    if (groupId is null) return await SaveAsync(trip.DetachGroup(), ct);
    var group = await groups.GetWithMembersAsync(groupId.Value, ct);
    if (group is null || !group.IsMember(userId)) return GroupErrors.NotFound;
    if (group.Status == GroupStatus.Closed) return GroupErrors.Closed;
    return await SaveAsync(trip.AttachGroup(group.Id), ct);
  }

  private async Task<Result> SaveAsync(Result r, CancellationToken ct)
  {
    if (r.IsFailure) return r;
    await uow.SaveChangesAsync(ct);
    return Result.Success();
  }

  /// <summary>Not-a-member always looks like "group not found" (the group's existence is not leaked).</summary>
  private async Task<Result> MutateAsync(Guid userId, Guid groupId, bool leaderOnly, Func<Group, Result> change, CancellationToken ct)
  {
    var group = await groups.GetWithMembersAsync(groupId, ct);
    if (group is null || !group.IsMember(userId)) return GroupErrors.NotFound;
    if (leaderOnly && group.LeaderId != userId) return GroupErrors.NotLeader;
    return await SaveAsync(change(group), ct);
  }

  private async Task<Result<GroupResponse>> MutateWithDtoAsync(Guid userId, Guid groupId, Func<Group, Result> change, CancellationToken ct)
  {
    var group = await groups.GetWithMembersAsync(groupId, ct);
    if (group is null || !group.IsMember(userId)) return GroupErrors.NotFound;
    var r = change(group);
    if (r.IsFailure) return r.Error;
    await uow.SaveChangesAsync(ct);
    return await DtoAsync(groupId, userId, ct);
  }

  private async Task<Result<GroupResponse>> DtoAsync(Guid groupId, Guid userId, CancellationToken ct) =>
    await read.GetAsync(groupId, userId, ct) ?? (Result<GroupResponse>)GroupErrors.NotFound;
}
