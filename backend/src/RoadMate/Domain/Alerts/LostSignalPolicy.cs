namespace RoadMate.Domain.Alerts;

public sealed class LostSignalPolicy : IAlertTypePolicy
{
  public AlertType Type => AlertType.LostSignal;
  public bool NotifiesContacts => true;
  public TimeSpan? RepeatInterval(AlertTiming t) => t.Slow;
  public string ReasonText => "mất tín hiệu quá lâu";
}
