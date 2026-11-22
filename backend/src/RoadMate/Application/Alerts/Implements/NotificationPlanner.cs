using Microsoft.Extensions.Options;
using RoadMate.Application.Abstractions.Persistence;
using RoadMate.Application.Alerts.Interfaces;
using RoadMate.Domain.Alerts;
using RoadMate.Domain.Common;

namespace RoadMate.Application.Alerts.Implements;

public class NotificationPlanner(IEmergencyContactRepository contacts, IAlertAckRepository acks, INotificationDeliveryRepository deliveries,
  ISimulationGate gate, IOptions<AlertOptions> options) : INotificationPlanner
{
  public async Task<PlanResult> PlanAsync(Alert alert, NotificationKind kind, int sequence, DateTimeOffset now, CancellationToken ct)
  {
    if (await deliveries.ExistsAsync(alert.Id, kind, sequence, ct)) return PlanResult.Empty;
    var recipients = await contacts.GetAlertRecipientsAsync(alert.TripId, alert.UserId, ct);
    if (recipients.Count == 0) return PlanResult.Empty;

    var existing = (await acks.ListByAlertAsync(alert.Id, ct)).ToList();
    var created = new List<AlertAck>();
    if (kind != NotificationKind.Closed)
      foreach (var r in recipients.Where(r => existing.All(a => a.ContactId != r.ContactId)))
        created.Add(AlertAck.Create(alert.Id, r.ContactId, TokenGenerator.NewToken(), now.AddHours(options.Value.AckLinkHours)));
    if (created.Count > 0) await acks.AddRangeAsync(created, ct);

    var acked = existing.Where(a => a.IsAcked).Select(a => a.ContactId).ToHashSet();
    var rows = new List<NotificationDelivery>();
    foreach (var r in recipients.Where(r => kind != NotificationKind.Repeat || !acked.Contains(r.ContactId)))
    {
      rows.Add(Row(alert, r.ContactId, DeliveryChannel.Sms, kind, sequence, r.Phone, now));
      if (!string.IsNullOrEmpty(r.LinkedFcmToken)) rows.Add(Row(alert, r.ContactId, DeliveryChannel.Push, kind, sequence, r.Phone, now));
    }
    await deliveries.AddRangeAsync(rows, ct);
    return new PlanResult(rows, created);
  }

  public async Task<bool> AllRecipientsAckedAsync(Alert alert, CancellationToken ct)
  {
    var recipients = await contacts.GetAlertRecipientsAsync(alert.TripId, alert.UserId, ct);
    if (recipients.Count == 0) return false;
    var acked = (await acks.ListByAlertAsync(alert.Id, ct)).Where(a => a.IsAcked).Select(a => a.ContactId).ToHashSet();
    return recipients.All(r => acked.Contains(r.ContactId));
  }

  private NotificationDelivery Row(Alert alert, Guid contactId, DeliveryChannel ch, NotificationKind kind, int seq, string phone, DateTimeOffset now)
  {
    var d = NotificationDelivery.Queue(alert.Id, contactId, ch, kind, seq, now);
    if (!gate.AllowsSend(alert, phone)) d.Skip("dry-run", now);
    return d;
  }
}
