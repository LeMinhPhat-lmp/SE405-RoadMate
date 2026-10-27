namespace RoadMate.Infrastructure;

/// <summary>
/// Job nền: lặp gửi cảnh báo mỗi N phút (phương án dự phòng khi app offline).
/// Chạy trong tiến trình API: nếu scale nhiều instance sẽ gửi trùng SMS (xem docs/SETUP_AND_LIMITATIONS.md).
/// </summary>
public class AlertRepeatWorker(IServiceScopeFactory scopes, ILogger<AlertRepeatWorker> log) : BackgroundService
{
  protected override async Task ExecuteAsync(CancellationToken stoppingToken)
  {
    while (!stoppingToken.IsCancellationRequested)
    {
      try
      {
        using var scope = scopes.CreateScope();
        var svc = scope.ServiceProvider.GetRequiredService<IAlertService>();
        await svc.ProcessDueRepeatsAsync(stoppingToken);
      }
      catch (Exception ex) when (ex is not OperationCanceledException) { log.LogError(ex, "AlertRepeatWorker lỗi"); }
      await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);
    }
  }
}
