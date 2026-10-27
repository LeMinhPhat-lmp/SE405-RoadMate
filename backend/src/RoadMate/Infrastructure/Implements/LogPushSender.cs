namespace RoadMate.Infrastructure.Implements;

/// <summary>Dùng khi chưa cấu hình Firebase:CredentialPath: chỉ ghi log, KHÔNG gửi push thật.</summary>
public class LogPushSender(ILogger<LogPushSender> log) : IPushSender
{
  public Task<Result<string>> SendAsync(string fcmToken, string title, string body, IDictionary<string, string>? data = null, CancellationToken ct = default)
  {
    log.LogWarning("[PUSH-LOG] {Title}: {Body}", title, body);
    return Task.FromResult(Result.Success("log-push"));
  }
}
