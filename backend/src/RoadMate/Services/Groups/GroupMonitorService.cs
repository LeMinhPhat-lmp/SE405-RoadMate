using Microsoft.Extensions.Caching.Memory;
using RoadMate.Infrastructure.Notifications;

namespace RoadMate.Services.Groups;

/// <summary>
/// Tụt đoàn: thành viên cách trưởng đoàn &gt; StragglerMeters LIÊN TỤC &gt; StragglerSeconds (lọc nhiễu GPS) khi nhóm đang "riding"
/// → push cho trưởng đoàn và chính thành viên đó (không SMS). Mốc "bắt đầu tụt" và cooldown nằm trong bộ nhớ (một instance);
/// khởi động lại server thì đếm lại từ đầu.
/// </summary>
public class GroupMonitorService(IUnitOfWork uow, IPushSender push, IMemoryCache cache, IOptions<GroupOptions> options,
  TimeProvider clock, ILogger<GroupMonitorService> log) : IGroupMonitorService
{
  private const string PurgeKey = "group:purge";
  private static readonly TimeSpan PurgeEvery = TimeSpan.FromHours(1);
  /// <summary>Quá thời gian này mà không còn đo được thì quên mốc "bắt đầu tụt".</summary>
  private static readonly TimeSpan StateTtl = TimeSpan.FromMinutes(30);

  public async Task RunOnceAsync(CancellationToken ct)
  {
    var now = clock.UtcNow();
    await PurgeOldMessagesAsync(now, ct);
    foreach (var group in await uow.GroupRepository.ListRidingAsync(ct))
    {
      try { await CheckStragglersAsync(group, now, ct); }
      catch (Exception ex) when (ex is not OperationCanceledException)
      {
        log.LogError(ex, "Kiểm tra tụt đoàn của nhóm {GroupId} lỗi, bỏ qua", group.Id);
      }
    }
  }

  private async Task PurgeOldMessagesAsync(DateTime now, CancellationToken ct)
  {
    if (cache.TryGetValue(PurgeKey, out _)) return;              // mỗi giờ một lần là đủ
    cache.Set(PurgeKey, true, PurgeEvery);
    var deleted = await uow.GroupMessageRepository.DeleteOlderThanAsync(now.AddDays(-options.Value.MessageRetentionDays), ct);
    if (deleted > 0) log.LogInformation("Đã xoá {Count} tin nhắn nhóm quá {Days} ngày", deleted, options.Value.MessageRetentionDays);
  }

  private async Task CheckStragglersAsync(Group group, DateTime now, CancellationToken ct)
  {
    var o = options.Value;
    var leader = group.Members.FirstOrDefault(m => m.UserId == group.LeaderId);
    if (!IsFresh(leader, now, o)) return;

    foreach (var member in group.Members.Where(m => m.UserId != group.LeaderId && IsFresh(m, now, o)))
    {
      var distance = Geo.HaversineMeters(leader!.LastLat!.Value, leader.LastLng!.Value, member.LastLat!.Value, member.LastLng!.Value);
      var sinceKey = $"group:straggler:{group.Id}:{member.UserId}";
      if (distance <= o.StragglerMeters) { cache.Remove(sinceKey); continue; }

      if (!cache.TryGetValue(sinceKey, out DateTime since)) { since = now; cache.Set(sinceKey, since, StateTtl); }
      if (now - since < TimeSpan.FromSeconds(o.StragglerSeconds)) continue;

      var cooldownKey = $"group:straggler-notified:{group.Id}:{member.UserId}";
      if (cache.TryGetValue(cooldownKey, out _)) continue;
      cache.Set(cooldownKey, true, TimeSpan.FromMinutes(o.NotifyCooldownMinutes));
      await NotifyStragglerAsync(group, leader, member, distance, ct);
    }
  }

  private static bool IsFresh(GroupMember? m, DateTime now, GroupOptions o) =>
    m is { LastLat: not null, LastLng: not null, LocationUpdatedAt: { } at } && now - at <= TimeSpan.FromSeconds(o.LocationFreshSeconds);

  private async Task NotifyStragglerAsync(Group group, GroupMember leader, GroupMember member, double distanceM, CancellationToken ct)
  {
    var km = distanceM / 1000;
    var name = member.User?.Name ?? "Một thành viên";
    var data = new Dictionary<string, string>
    {
      ["type"] = "group_straggler", ["groupId"] = group.Id.ToString(), ["memberId"] = member.UserId.ToString(), ["distanceM"] = ((int)distanceM).ToString()
    };
    var targets = new[]
    {
      (Token: leader.User?.FcmToken, Title: "RoadMate: có người tụt đoàn", Body: $"{name} đang cách bạn khoảng {km:0.#} km."),
      (Token: member.User?.FcmToken, Title: "RoadMate: bạn đang tụt đoàn", Body: $"Bạn đang cách trưởng đoàn khoảng {km:0.#} km, hãy bắt kịp đoàn.")
    };
    var sent = 0;
    foreach (var t in targets.Where(t => !string.IsNullOrEmpty(t.Token)))
      if ((await push.SendAsync(t.Token!, t.Title, t.Body, data, ct)).IsSuccess) sent++;
    log.LogWarning("Nhóm {GroupId}: thành viên {MemberId} tụt đoàn {Km:0.0} km, đã push {Sent} người", group.Id, member.UserId, km, sent);
  }
}
