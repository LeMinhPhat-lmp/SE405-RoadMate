using RoadMate.Application.Abstractions.Messaging;
using RoadMate.Application.Abstractions.Persistence;
using RoadMate.Domain.Alerts;
using RoadMate.Domain.Common;

namespace RoadMate.Application.Notifications;

/// <summary>Best-effort push to app users (owner reminders, group events). Never throws: a failed push must not break the caller.</summary>
public class PushNotifier(IUserRepository users, IEnumerable<INotificationChannel> channels, ILogger<PushNotifier> log) : IPushNotifier
{
  public async Task<int> NotifyUsersAsync(IReadOnlyCollection<Guid> userIds, PushContent content, CancellationToken ct = default)
  {
    if (userIds.Count == 0) return 0;
    var tokens = await users.GetFcmTokensAsync(userIds, ct);
    var sent = 0;
    foreach (var token in tokens.Values)
      if ((await NotifyTokenAsync(token, content, ct)).IsSuccess) sent++;
    if (sent < userIds.Count) log.LogDebug("Push: {Sent}/{Total} người nhận (thiếu FCM token hoặc gửi lỗi)", sent, userIds.Count);
    return sent;
  }

  public async Task<Result<string>> NotifyTokenAsync(string token, PushContent content, CancellationToken ct = default)
  {
    var channel = channels.FirstOrDefault(c => c.Channel == DeliveryChannel.Push);
    if (channel is null) return Error.NotConfigured("PUSH_NOT_CONFIGURED", "Chưa cấu hình kênh push.");
    try { return await channel.SendAsync(new NotificationMessage(token, content.Title, content.Body, content.Data), ct); }
    catch (Exception ex) when (ex is not OperationCanceledException) { log.LogWarning(ex, "Push lỗi không mong đợi"); return Error.External("PUSH_FAILED", "Gửi push lỗi."); }
  }
}
