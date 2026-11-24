namespace RoadMate.Domain.Alerts;

public sealed class SosPolicy : IAlertTypePolicy
{
  public AlertType Type => AlertType.Sos;
  public bool NotifiesContacts => true;
  public TimeSpan? RepeatInterval(AlertTiming t) => t.Fast;
  public string ReasonText => "vừa bấm nút SOS";
}
