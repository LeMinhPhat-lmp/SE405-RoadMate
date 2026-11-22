using RoadMate.Domain.Common;

namespace RoadMate.Infrastructure.Notifications;

public interface IPushProvider
{
  Task<Result<string>> SendAsync(string fcmToken, string title, string body, IDictionary<string, string>? data = null, CancellationToken ct = default);
}
