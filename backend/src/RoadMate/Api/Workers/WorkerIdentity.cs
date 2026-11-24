namespace RoadMate.Api.Workers;

public static class WorkerIdentity
{
  public static string New(string kind) => $"{kind}:{Environment.MachineName}:{Environment.ProcessId}:{Guid.NewGuid().ToString("N")[..6]}";
}
