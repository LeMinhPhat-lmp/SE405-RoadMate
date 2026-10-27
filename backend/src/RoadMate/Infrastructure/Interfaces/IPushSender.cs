namespace RoadMate.Infrastructure.Interfaces;

public interface IPushSender
{
  Task<Result<string>> SendAsync(string fcmToken, string title, string body, IDictionary<string, string>? data = null, CancellationToken ct = default);
}
