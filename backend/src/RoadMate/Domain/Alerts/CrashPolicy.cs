namespace RoadMate.Domain.Alerts;

public sealed class CrashPolicy : IAlertTypePolicy
{
  public AlertType Type => AlertType.Crash;
  public bool NotifiesContacts => true;
  public TimeSpan? RepeatInterval(AlertTiming t) => t.Fast;
  public string ReasonText => "nghi gặp va chạm/té ngã và không phản hồi";
}
