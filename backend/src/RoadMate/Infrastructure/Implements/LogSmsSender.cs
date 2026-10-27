namespace RoadMate.Infrastructure.Implements;

/// <summary>Mặc định (dev): chỉ ghi log, KHÔNG gửi SMS thật.</summary>
public class LogSmsSender(ILogger<LogSmsSender> log) : ISmsSender
{
  public Task<Result<string>> SendAsync(string phone, string message, CancellationToken ct = default)
  {
    log.LogWarning("[SMS-LOG] to {Phone}: {Message}", phone, message);
    return Task.FromResult(Result.Success("log-" + Guid.NewGuid().ToString("N")[..8]));
  }
}
