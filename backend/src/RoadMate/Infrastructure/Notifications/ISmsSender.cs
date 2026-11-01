namespace RoadMate.Infrastructure.Notifications;

public interface ISmsSender
{
  Task<Result<string>> SendAsync(string phone, string message, CancellationToken ct = default);
}
