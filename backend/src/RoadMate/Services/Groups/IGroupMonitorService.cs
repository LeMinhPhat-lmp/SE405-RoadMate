namespace RoadMate.Services.Groups;

/// <summary>Job nền của nhóm: cảnh báo tụt đoàn và xoá tin nhắn quá hạn.</summary>
public interface IGroupMonitorService
{
  Task RunOnceAsync(CancellationToken ct);
}
