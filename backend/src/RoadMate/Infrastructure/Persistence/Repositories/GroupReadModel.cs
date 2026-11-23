using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RoadMate.Application.Abstractions.Persistence;
using RoadMate.Application.Common;
using RoadMate.Application.Groups;
using RoadMate.Application.Groups.DTOs.Responses;
using RoadMate.Application.Groups.Mappers;
using RoadMate.Domain.Common;
using RoadMate.Domain.Groups;

namespace RoadMate.Infrastructure.Persistence.Repositories;

public class GroupReadModel(AppDbContext db, IOptions<GroupOptions> options) : IGroupReadModel
{
  private int MaxMembers => options.Value.MaxMembers;

  public async Task<IReadOnlyList<GroupListItemResponse>> ListForUserAsync(Guid userId, CancellationToken ct = default)
  {
    var groups = await db.Groups.AsNoTracking().Include(g => g.Members).AsSplitQuery()
      .Where(g => g.Members.Any(m => m.UserId == userId) && g.Status != GroupStatus.Closed)
      .OrderByDescending(g => g.CreatedAt).ThenByDescending(g => g.Id).ToListAsync(ct);
    return groups.Select(g => new GroupListItemResponse(g.Id, g.Name, EnumWire.ToWire(g.Status), EnumWire.ToWire(g.Members.First(m => m.UserId == userId).Role),
      g.Members.Count, g.DepartAt.ToWire())).ToList();
  }

  public async Task<GroupResponse?> GetAsync(Guid groupId, Guid userId, CancellationToken ct = default)
  {
    var g = await Load(groupId, ct);
    return g is not null && g.IsMember(userId) ? g.ToDto(MaxMembers) : null;
  }

  public async Task<GroupSnapshotResponse?> GetSnapshotAsync(Guid groupId, Guid userId, DateTimeOffset? since, int max, CancellationToken ct = default)
  {
    var g = await Load(groupId, ct);
    if (g is null || !g.IsMember(userId)) return null;
    var messages = since is null ? [] : await ListMessagesAsync(groupId, since, max, ct);
    return new GroupSnapshotResponse(g.Id, EnumWire.ToWire(g.Status), g.DepartAt.ToWire(), g.ToMemberDtos(), messages.ToList());
  }

  public async Task<IReadOnlyList<GroupMessageResponse>> ListMessagesAsync(Guid groupId, DateTimeOffset? since, int max, CancellationToken ct = default)
  {
    var q = db.GroupMessages.AsNoTracking().Include(m => m.Sender).Where(m => m.GroupId == groupId);
    if (since is { } s) q = q.Where(m => m.CreatedAt > s);
    return (await q.OrderBy(m => m.CreatedAt).ThenBy(m => m.Id).Take(max).ToListAsync(ct)).Select(m => m.ToDto()).ToList();
  }

  public async Task<CursorPage<GroupMessageResponse>> ListMessagesPageAsync(Guid groupId, CursorPosition? after, int limit, CancellationToken ct = default)
  {
    var take = CursorPagination.NormalizeLimit(limit);
    IQueryable<GroupMessage> q = db.GroupMessages.AsNoTracking().Include(m => m.Sender).Where(m => m.GroupId == groupId);

    if (after is not null)
    {
      var afterAt = after.SortAt;
      var afterId = after.Id;
      q = q.Where(m => m.CreatedAt > afterAt || (m.CreatedAt == afterAt && m.Id.CompareTo(afterId) > 0));
    }

    var rows = await q.OrderBy(m => m.CreatedAt).ThenBy(m => m.Id).Take(take + 1).ToListAsync(ct);
    var page = CursorPagination.ToPage(rows, take, m => new CursorPosition(m.CreatedAt, m.Id));
    return new CursorPage<GroupMessageResponse>(page.Items.Select(m => m.ToDto()).ToList(), page.NextCursor, page.HasMore);
  }

  private Task<Group?> Load(Guid id, CancellationToken ct) =>
    db.Groups.AsNoTracking().Include(g => g.Members).ThenInclude(m => m.User).AsSplitQuery().FirstOrDefaultAsync(g => g.Id == id, ct);
}
