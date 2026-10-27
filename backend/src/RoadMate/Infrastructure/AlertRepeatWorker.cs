namespace RoadMate.Infrastructure;

/// <summary>Job nền: lặp gửi cảnh báo mỗi N phút (server là phương án dự phòng khi app offline).</summary>
public class AlertRepeatWorker : BackgroundService
{
  private readonly IServiceScopeFactory _scopes;
  private readonly ILogger<AlertRepeatWorker> _log;
  public AlertRepeatWorker(IServiceScopeFactory scopes, ILogger<AlertRepeatWorker> log) { _scopes = scopes; _log = log; }

  protected override async Task ExecuteAsync(CancellationToken stoppingToken)
  {
    while (!stoppingToken.IsCancellationRequested)
    {
      try
      {
        using var scope = _scopes.CreateScope();
        var svc = scope.ServiceProvider.GetRequiredService<IAlertService>();
        await svc.ProcessDueRepeatsAsync(stoppingToken);
      }
      catch (Exception ex) when (ex is not OperationCanceledException) { _log.LogError(ex, "AlertRepeatWorker lỗi"); }
      await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);
    }
  }
}
