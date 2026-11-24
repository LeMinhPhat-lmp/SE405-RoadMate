using Microsoft.Extensions.Options;
using RoadMate.Application.Abstractions.Messaging;
using RoadMate.Application.Abstractions.Persistence;
using RoadMate.Application.Groups.Interfaces;
using RoadMate.Domain.Common;
using RoadMate.Domain.Groups;

namespace RoadMate.Application.Groups.Implements;

/// <summary>
/// Straggler = a member farther than <c>StragglerMeters</c> from the leader CONTINUOUSLY for <c>StragglerSeconds</c> while the group is riding.
/// The "since" mark is persisted on the member (<c>StragglingSince</c>), so a restart does not forget it; the notify cooldown is in memory.
/// </summary>
public class GroupMonitorService(IGroupRepository groups, IGroupMessageRepository messages, IUnitOfWork uow, IPushNotifier push, ICooldown cooldown,
  IOptions<GroupOptions> options, TimeProvider clock, ILogger<GroupMonitorService> log) : IGroupMonitorService
{
  public async Task RunOnceAsync(CancellationToken ct)
  {
    var now = clock.GetUtcNow();
    await PurgeAsync(now, ct);
    foreach (var group in await groups.ListRidingAsync(ct))
    {
      try { await CheckAsync(group, now, ct); }
      catch (Exception ex) when (ex is not OperationCanceledException)
      {
        log.LogError(ex, "Kiểm tra tụt đoàn của nhóm {GroupId} lỗi, bỏ qua", group.Id);
        uow.DiscardChanges();
      }
    }
  }

  private async Task PurgeAsync(DateTimeOffset now, CancellationToken ct)
  {
    if (!cooldown.TryAcquire("group:purge", TimeSpan.FromHours(1))) return;
    var deleted = await messages.PurgeOlderThanAsync(now.AddDays(-options.Value.MessageRetentionDays), ct);
    if (deleted > 0) log.LogInformation("Đã xoá {Count} tin nhắn nhóm quá {Days} ngày", deleted, options.Value.MessageRetentionDays);
  }

  private async Task CheckAsync(Group group, DateTimeOffset now, CancellationToken ct)
  {
    var o = options.Value;
    var leader = group.LeaderMember;
    if (!IsFresh(leader, now, o)) return;
    foreach (var member in group.Members.Where(m => m.UserId != group.LeaderId && IsFresh(m, now, o)))
    {
      var distance = Geo.HaversineMeters(leader!.LastLat!.Value, leader.LastLng!.Value, member.LastLat!.Value, member.LastLng!.Value);
      var since = group.UpdateStraggling(member.UserId, distance > o.StragglerMeters, now);
      if (since is null || now - since < TimeSpan.FromSeconds(o.StragglerSeconds)) continue;
      if (!cooldown.TryAcquire($"group:straggler:{group.Id}:{member.UserId}", TimeSpan.FromMinutes(o.NotifyCooldownMinutes))) continue;
      await NotifyAsync(group, leader, member, distance, ct);
    }
    await uow.SaveChangesAsync(ct);      // persists StragglingSince changes
  }

  private static bool IsFresh(GroupMember? m, DateTimeOffset now, GroupOptions o) =>
    m is { LastLat: not null, LastLng: not null, LocationUpdatedAt: { } at } && now - at <= TimeSpan.FromSeconds(o.LocationFreshSeconds);

  private async Task NotifyAsync(Group group, GroupMember leader, GroupMember member, double distanceM, CancellationToken ct)
  {
    var km = distanceM / 1000;
    var name = member.User?.Name ?? "Một thành viên";
    var data = new Dictionary<string, string>
    {
      ["type"] = "group_straggler",
      ["groupId"] = group.Id.ToString(),
      ["memberId"] = member.UserId.ToString(),
      ["distanceM"] = ((int)distanceM).ToString(System.Globalization.CultureInfo.InvariantCulture)
    };
    var sent = await push.NotifyUsersAsync([leader.UserId], new PushContent("RoadMate: có người tụt đoàn", $"{name} đang cách bạn khoảng {km:0.#} km.", data), ct);
    sent += await push.NotifyUsersAsync([member.UserId], new PushContent("RoadMate: bạn đang tụt đoàn", $"Bạn đang cách trưởng đoàn khoảng {km:0.#} km, hãy bắt kịp đoàn.", data), ct);
    log.LogWarning("Nhóm {GroupId}: thành viên {MemberId} tụt đoàn {Km:0.0} km, đã push {Sent} người", group.Id, member.UserId, km, sent);
  }
}
