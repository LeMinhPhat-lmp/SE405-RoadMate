namespace RoadMate.Infrastructure.Notifications;

public class SmsOptions
{
  public string Provider { get; set; } = "log";
  public bool StripDiacritics { get; set; } = true;
  public ESmsOptions ESms { get; set; } = new();
}
