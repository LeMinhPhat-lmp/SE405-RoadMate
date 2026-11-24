namespace RoadMate.Domain.Alerts;

public sealed class LowBatteryPolicy : IAlertTypePolicy
{
  public AlertType Type => AlertType.LowBattery;
  public bool NotifiesContacts => true;
  public TimeSpan? RepeatInterval(AlertTiming t) => null;
  public string ReasonText => "sắp hết pin";
}
