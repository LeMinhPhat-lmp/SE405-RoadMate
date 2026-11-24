using RoadMate.Domain.Common;

namespace RoadMate.Infrastructure.Notifications;

/// <summary>Vendor SMS gateway (eSMS, log). Internal to Infrastructure: the rest of the system only knows <c>INotificationChannel</c>.</summary>
public interface ISmsProvider
{
  Task<Result<string>> SendAsync(string phone, string message, CancellationToken ct = default);
}
