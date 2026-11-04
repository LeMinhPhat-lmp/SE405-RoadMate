namespace RoadMate.Infrastructure.Notifications;

/// <summary>Dùng khi chưa nạp được Firebase credential: chỉ ghi log, KHÔNG gửi push thật.</summary>
public class LogPushSender(ILogger<LogPushSender> log) : IPushSender
{
  public Task<Result<string>> SendAsync(string fcmToken, string title, string body, IDictionary<string, string>? data = null, CancellationToken ct = default)
  {
    log.LogWarning("[PUSH-LOG] {Title}: {Body}", title, body);
    return Task.FromResult(Result.Success("log-push"));
  }
}
