using RoadMate.Domain.Abstractions;
using RoadMate.Domain.Alerts;
using RoadMate.Domain.Common;

namespace RoadMate.Application.Abstractions.Messaging;

/// <summary>One delivery channel (sms, push). Adding a channel = one class + one DI line.</summary>
public interface INotificationChannel
{
  DeliveryChannel Channel { get; }
  /// <summary>Returns the provider message id.</summary>
  Task<Result<string>> SendAsync(NotificationMessage message, CancellationToken ct);
}
