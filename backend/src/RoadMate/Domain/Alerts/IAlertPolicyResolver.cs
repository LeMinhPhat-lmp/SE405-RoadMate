namespace RoadMate.Domain.Alerts;

public interface IAlertPolicyResolver
{
  IAlertTypePolicy For(AlertType type);
}
