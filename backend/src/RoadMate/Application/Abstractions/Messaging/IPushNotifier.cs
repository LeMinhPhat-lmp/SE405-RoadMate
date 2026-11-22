using RoadMate.Domain.Abstractions;
using RoadMate.Domain.Alerts;
using RoadMate.Domain.Common;

namespace RoadMate.Application.Abstractions.Messaging;

/// <summary>Best-effort push to app users (monitors, groups). Failures are swallowed and counted.</summary>
public interface IPushNotifier
{
  Task<int> NotifyUsersAsync(IReadOnlyCollection<Guid> userIds, PushContent content, CancellationToken ct = default);
  Task<Result<string>> NotifyTokenAsync(string token, PushContent content, CancellationToken ct = default);
}
