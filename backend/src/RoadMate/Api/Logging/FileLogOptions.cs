namespace RoadMate.Api.Logging;

public class FileLogOptions
{
  public bool Enabled { get; set; } = true;
  public string Path { get; set; } = "logs/roadmate-.log";
  public int RetainedFiles { get; set; } = 14;
}
