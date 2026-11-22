using Microsoft.Extensions.Options;
using RoadMate.Application.Abstractions.Messaging;
using RoadMate.Application.Abstractions.Persistence;
using RoadMate.Application.Alerts.Interfaces;
using RoadMate.Domain.Alerts;
using RoadMate.Domain.Trips;

namespace RoadMate.Application.Alerts.Implements;

public class AlertRepeatService(IAlertRepository alerts, ITripRepository trips, INotificationPlanner planner, IAlertPolicyResolver policies,
  IUnitOfWork uow, IOptions<AlertOptions> options, TimeProvider clock) : IAlertRepeatService
{
  public async Task RepeatAsync(Guid alertId, CancellationToken ct)
  {
    var alert = await alerts.GetByIdAsync(alertId, ct);
    if (alert is null || !alert.IsOpen) return;
    var o = options.Value; var now = clock.GetUtcNow();
    var trip = await trips.GetByIdAsync(alert.TripId, ct);
    var finished = trip is null || trip.Status == TripStatus.Finished;
    var allAcked = !finished && await planner.AllRecipientsAckedAsync(alert, ct);
    var interval = policies.For(alert.Type).RepeatInterval(o.Timing);

    var decision = alert.PlanRepeat(now, interval, o.MaxRepeats, allAcked, finished);
    if (decision == RepeatDecision.Send) await planner.PlanAsync(alert, NotificationKind.Repeat, alert.RepeatCount, now, ct);
    await uow.SaveChangesAsync(ct);   // AlertClosed (finished trip) → handler plans the "safe" notice in the same transaction
  }
}
