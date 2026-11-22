using RoadMate.Application.Abstractions.Persistence;
using RoadMate.Application.Alerts.Interfaces;
using RoadMate.Application.Common;
using RoadMate.Domain.Alerts;
using RoadMate.Domain.Common;

namespace RoadMate.Application.Alerts.Implements;

public class AlertRaiser(IAlertRepository alerts, IAlertPolicyResolver policies, INotificationPlanner planner, IUnitOfWork uow, AlertTiming timing) : IAlertRaiser
{
  public static readonly Error ClientIdTaken = Error.Conflict("CLIENT_ID_TAKEN", "clientId đã được dùng bởi người dùng khác.");

  public async Task<Result<RaisedAlert>> RaiseAsync(RaiseAlertCommand cmd, DateTimeOffset now, CancellationToken ct)
  {
    var existing = await alerts.GetByClientIdAsync(cmd.ClientId.Value, ct);
    if (existing is not null) return Existing(existing, cmd.UserId);

    var policy = policies.For(cmd.Type);
    var alert = Alert.Raise(cmd.TripId, cmd.UserId, cmd.ClientId, cmd.Type, cmd.At, cmd.Battery, cmd.Simulation, policy, policy.RepeatInterval(timing), now);
    await alerts.AddAsync(alert, ct);

    var plan = PlanResult.Empty;
    if (policy.NotifiesContacts)
    {
      plan = await planner.PlanAsync(alert, NotificationKind.Initial, 0, now, ct);
      alert.MarkInitialDispatched(plan.Deliveries.Count > 0, allAcked: false);
    }

    try { await uow.SaveChangesAsync(ct); }
    catch (UniqueViolationException)
    {
      // A parallel request with the same clientId won the unique index: return the winner, send nothing twice.
      uow.DiscardChanges();
      var winner = await alerts.GetByClientIdAsync(cmd.ClientId.Value, ct);
      return winner is null ? Errors.AlertNotFound : Existing(winner, cmd.UserId);
    }
    return new RaisedAlert(alert, true, plan);
  }

  private static Result<RaisedAlert> Existing(Alert a, Guid userId) =>
    a.UserId == userId ? new RaisedAlert(a, false, PlanResult.Empty) : ClientIdTaken;
}
