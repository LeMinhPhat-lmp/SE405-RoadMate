namespace RoadMate.Infrastructure.Background;

/// <summary>
/// Khung chung cho job nền chạy định kỳ: mỗi lần chạy có DI scope riêng, lỗi của một lần chạy chỉ được log và
/// không làm worker chết. Chạy trong tiến trình API (chỉ an toàn với MỘT instance, xem docs/V3_NOTES.md).
/// </summary>
public abstract class PeriodicWorker(IServiceScopeFactory scopes, ILogger log) : BackgroundService
{
  protected abstract string Name { get; }
  protected abstract TimeSpan Interval { get; }
  protected abstract Task RunAsync(IServiceProvider services, CancellationToken ct);

  protected override async Task ExecuteAsync(CancellationToken stoppingToken)
  {
    log.LogInformation("Worker {Worker} khởi động, chu kỳ {Interval}", Name, Interval);
    using var timer = new PeriodicTimer(Interval);
    try
    {
      do { await TickAsync(stoppingToken); }
      while (await timer.WaitForNextTickAsync(stoppingToken));
    }
    catch (OperationCanceledException) { /* tắt app */ }
    log.LogInformation("Worker {Worker} đã dừng", Name);
  }

  private async Task TickAsync(CancellationToken ct)
  {
    try
    {
      using var scope = scopes.CreateScope();
      await RunAsync(scope.ServiceProvider, ct);
    }
    catch (Exception ex) when (ex is not OperationCanceledException)
    {
      log.LogError(ex, "Worker {Worker} lỗi ở lần chạy này, sẽ thử lại ở chu kỳ sau", Name);
    }
  }
}
