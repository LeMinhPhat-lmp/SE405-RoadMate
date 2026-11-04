namespace RoadMate.Services.Monitoring;

/// <summary>Giám sát các chuyến đang chạy: phát hiện "sự im lặng" mà chỉ server mới biết (mất sóng, về trễ).</summary>
public interface ITripMonitorService
{
  Task RunOnceAsync(CancellationToken ct);
}
