namespace RoadMate.Domain.Alerts;

/// <summary>offroute only reminds the owner in the app: never SMS relatives.</summary>
public sealed class OffRoutePolicy : IAlertTypePolicy
{
  public AlertType Type => AlertType.OffRoute;
  public bool NotifiesContacts => false;
  public TimeSpan? RepeatInterval(AlertTiming t) => null;
  public string ReasonText => "đang lệch khỏi lộ trình";
}
