namespace RoadMate.Domain.Alerts;

/// <summary>Strategy per alert type (OCP): add a type = one enum value + one policy class.</summary>
public interface IAlertTypePolicy
{
  AlertType Type { get; }
  bool NotifiesContacts { get; }
  /// <summary>null = never repeat.</summary>
  TimeSpan? RepeatInterval(AlertTiming timing);
  /// <summary>Vietnamese reason used in SMS/push ("... nghi gặp va chạm/té ngã và không phản hồi").</summary>
  string ReasonText { get; }
}
