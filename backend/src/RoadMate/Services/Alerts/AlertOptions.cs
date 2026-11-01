namespace RoadMate.Services.Alerts;

public class AlertOptions
{
  public int RepeatMinutes { get; set; } = 2;
  public int MaxRepeats { get; set; } = 30;
}
