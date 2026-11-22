namespace RoadMate.Application.Groups.Interfaces;

/// <summary>Background job of groups: straggler warnings and purging old messages.</summary>
public interface IGroupMonitorService
{
  Task RunOnceAsync(CancellationToken ct);
}
