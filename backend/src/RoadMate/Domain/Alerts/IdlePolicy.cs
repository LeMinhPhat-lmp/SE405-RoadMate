namespace RoadMate.Domain.Alerts;

public sealed class IdlePolicy : IAlertTypePolicy
{
  public AlertType Type => AlertType.Idle;
  public bool NotifiesContacts => true;
  public TimeSpan? RepeatInterval(AlertTiming t) => t.Fast;
  public string ReasonText => "dừng quá lâu và không phản hồi";
}
