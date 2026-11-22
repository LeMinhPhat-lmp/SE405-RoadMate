namespace RoadMate.Domain.Alerts;

public sealed class LateReturnPolicy : IAlertTypePolicy
{
  public AlertType Type => AlertType.LateReturn;
  public bool NotifiesContacts => true;
  public TimeSpan? RepeatInterval(AlertTiming t) => t.Slow;
  public string ReasonText => "chưa về dù đã quá giờ dự kiến";
}
