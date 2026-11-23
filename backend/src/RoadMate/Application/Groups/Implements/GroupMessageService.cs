using Microsoft.Extensions.Options;
using RoadMate.Application.Abstractions.Messaging;
using RoadMate.Application.Abstractions.Persistence;
using RoadMate.Application.Common;
using RoadMate.Application.Groups.DTOs.Requests;
using RoadMate.Application.Groups.DTOs.Responses;
using RoadMate.Application.Groups.Interfaces;
using RoadMate.Application.Shared;
using RoadMate.Domain.Common;
using RoadMate.Domain.Groups;

namespace RoadMate.Application.Groups.Implements;

public class GroupMessageService(IGroupRepository groups, IGroupMessageRepository messages, IGroupReadModel read, IUnitOfWork uow, IPushNotifier push,
  IOptions<GroupOptions> options, TimeProvider clock) : IGroupMessageService
{
  public async Task<Result<GroupMessageResponse>> SendMessageAsync(Guid userId, Guid groupId, SendGroupMessageRequest req, CancellationToken ct)
  {
    var group = await groups.GetWithMembersAsync(groupId, ct);
    if (group is null || !group.IsMember(userId)) return GroupErrors.NotFound;
    var posted = group.PostQuickMessage(userId, req.QuickType);
    if (posted.IsFailure) return posted.Error;
    await messages.AddAsync(posted.Value, ct);
    await uow.SaveChangesAsync(ct);

    var senderName = group.MemberOf(userId)?.User?.Name ?? "";
    var label = QuickMessages.Label(req.QuickType);
    if (QuickMessages.PushesToGroup(req.QuickType))
    {
      var others = group.Members.Where(m => m.UserId != userId).Select(m => m.UserId).ToList();
      await push.NotifyUsersAsync(others, new PushContent($"RoadMate nhóm: {label}", $"{senderName}: {label}",
        new Dictionary<string, string> { ["type"] = "group_message", ["groupId"] = groupId.ToString(), ["quickType"] = req.QuickType, ["senderName"] = senderName }), ct);
    }
    return new GroupMessageResponse(posted.Value.Id, userId, senderName, req.QuickType, label, posted.Value.CreatedAt == default ? clock.GetUtcNow() : posted.Value.CreatedAt);
  }

  public async Task<Result<List<GroupMessageResponse>>> ListMessagesAsync(Guid userId, Guid groupId, DateTime? since, CancellationToken ct)
  {
    var group = await groups.GetWithMembersAsync(groupId, ct);
    if (group is null || !group.IsMember(userId)) return GroupErrors.NotFound;
    return (await read.ListMessagesAsync(groupId, since.ToUtcOffset(), options.Value.MaxMessagesPerPoll, ct)).ToList();
  }

  public async Task<Result<CursorPageResponse<GroupMessageResponse>>> ListMessagesPageAsync(Guid userId, Guid groupId, string? cursor, int? limit, CancellationToken ct)
  {
    if (!CursorPosition.TryDecode(cursor, out var after)) return CursorPagination.InvalidCursor;
    var group = await groups.GetWithMembersAsync(groupId, ct);
    if (group is null || !group.IsMember(userId)) return GroupErrors.NotFound;
    var page = await read.ListMessagesPageAsync(groupId, after, CursorPagination.NormalizeLimit(limit), ct);
    return new CursorPageResponse<GroupMessageResponse>(page.Items, page.NextCursor, page.HasMore);
  }
}
