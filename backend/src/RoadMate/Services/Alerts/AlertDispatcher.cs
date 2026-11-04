using RoadMate.Services.Notifications;

namespace RoadMate.Services.Alerts;

public class AlertDispatcher(IContactNotifier notifier, IUnitOfWork uow, IGeocodingService geocoding,
  IOptions<TrackingOptions> tracking, TimeProvider clock) : IAlertDispatcher
{
  /// <summary>Địa chỉ chỉ là phần "thêm cho dễ tìm": quá thời gian này thì gửi tin không kèm địa chỉ, không làm chậm cảnh báo.</summary>
  private static readonly TimeSpan AddressTimeout = TimeSpan.FromSeconds(3);

  public async Task<DispatchResult> DispatchAsync(Alert alert, User user, IReadOnlyList<EmergencyContact> contacts, bool isRepeat, CancellationToken ct)
  {
    var acks = await EnsureAcksAsync(alert, contacts, ct);
    var pending = contacts.Where(c => !acks[c.Id].IsAcked).ToList();
    if (pending.Count == 0) return new DispatchResult([], AllAcknowledged: contacts.Count > 0);

    var address = await TryGetAddressAsync(alert.Lat, alert.Lng, ct);
    var now = clock.UtcNow();
    var deliveries = await notifier.NotifyAsync(alert.Id, pending,
      c => AlertMessages.ForAlert(alert, user, isRepeat, now, address, TrackingUrl.Build(tracking.Value.AckUrl, acks[c.Id].Token)),
      ModeOf(alert), ct);
    await uow.AlertDeliveryRepository.AddRangeAsync(deliveries, ct);
    return new DispatchResult(deliveries, AllAcknowledged: false);
  }

  public async Task<List<AlertDelivery>> NotifyClosedAsync(Alert alert, User user, IReadOnlyList<EmergencyContact> contacts, CancellationToken ct)
  {
    var deliveries = await notifier.NotifyAsync(alert.Id, contacts, AlertMessages.Closed(alert, user), ModeOf(alert), ct);
    await uow.AlertDeliveryRepository.AddRangeAsync(deliveries, ct);
    return deliveries;
  }

  private static NotificationMode ModeOf(Alert a) => a.SimulationMode switch
  {
    null => NotificationMode.Live,
    SimulationMode.Live => NotificationMode.AllowListOnly,
    _ => NotificationMode.DryRun
  };

  /// <summary>Mỗi (cảnh báo, liên hệ) có đúng một token xác nhận, dùng lại cho các lần lặp.</summary>
  private async Task<Dictionary<Guid, AlertAck>> EnsureAcksAsync(Alert alert, IReadOnlyList<EmergencyContact> contacts, CancellationToken ct)
  {
    var map = (await uow.AlertAckRepository.ListByAlertAsync(alert.Id, ct)).ToDictionary(a => a.ContactId);
    var created = new List<AlertAck>();
    foreach (var c in contacts.Where(c => !map.ContainsKey(c.Id)))
    {
      var ack = new AlertAck { Id = Guid.NewGuid(), AlertId = alert.Id, ContactId = c.Id, Token = TokenGenerator.NewToken() };
      map[c.Id] = ack;
      created.Add(ack);
    }
    if (created.Count > 0) await uow.AlertAckRepository.AddRangeAsync(created, ct);
    return map;
  }

  /// <summary>Reverse geocode có timeout; mọi lỗi (chưa cấu hình key, hết hạn mức, timeout…) đều trả null – cảnh báo vẫn phải đi.</summary>
  private async Task<string?> TryGetAddressAsync(double lat, double lng, CancellationToken ct)
  {
    using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
    cts.CancelAfter(AddressTimeout);
    try
    {
      var r = await geocoding.ReverseAsync(lat, lng, cts.Token);
      return r.IsSuccess && !string.IsNullOrWhiteSpace(r.Value.Address) ? r.Value.Address : null;
    }
    catch (OperationCanceledException) when (!ct.IsCancellationRequested) { return null; }
  }
}
