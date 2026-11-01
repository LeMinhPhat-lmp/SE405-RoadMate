using RoadMate.Services.Alerts;

namespace RoadMate.Infrastructure.Background;

/// <summary>
/// Job nền: cứ 15 giây gọi IAlertRepeatProcessor để lặp gửi cảnh báo đến hạn (phương án dự phòng khi app offline).
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
        var processor = scope.ServiceProvider.GetRequiredService<IAlertRepeatProcessor>();
        await processor.ProcessDueAsync(stoppingToken);
      }
      catch (Exception ex) when (ex is not OperationCanceledException) { log.LogError(ex, "AlertRepeatWorker lỗi"); }
      await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);
    }
  }
}
