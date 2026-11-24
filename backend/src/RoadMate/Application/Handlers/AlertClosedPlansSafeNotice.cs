using RoadMate.Application.Abstractions.Messaging;
using RoadMate.Application.Abstractions.Persistence;
using RoadMate.Application.Alerts.Interfaces;
using RoadMate.Domain.Alerts;
using RoadMate.Domain.Events;

namespace RoadMate.Application.Handlers;

/// <summary>Alert closed after contacts were notified → plan the "đã an toàn / có tín hiệu trở lại" notices (same transaction).</summary>
public sealed class AlertClosedPlansSafeNotice(IAlertRepository alerts, INotificationDeliveryRepository deliveries, INotificationPlanner planner) : IDomainEventHandler<AlertClosed>
{
  public async Task HandleAsync(AlertClosed e, CancellationToken ct)
  {
    if (!e.WasNotified) return;
    // Only tell relatives "safe" if they were really told about the alert (something was actually sent, not just planned or simulated).
    if (!(await deliveries.ListByAlertAsync(e.AlertId, ct)).Any(d => d.Status == DeliveryStatus.Sent && d.Kind != NotificationKind.Closed)) return;
    var alert = await alerts.GetByIdAsync(e.AlertId, cancellationToken: ct);
    if (alert is not null) await planner.PlanAsync(alert, NotificationKind.Closed, 0, e.OccurredAt, ct);
  }
}
