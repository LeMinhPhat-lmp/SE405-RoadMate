using RoadMate.Services.Notifications;

namespace RoadMate.Services.Alerts;

public class AlertDispatcher(IContactNotifier notifier, IUnitOfWork uow, IGeocodingService geocoding) : IAlertDispatcher
{
  /// <summary>Địa chỉ chỉ là phần "thêm cho dễ tìm": quá thời gian này thì gửi tin không kèm địa chỉ, không làm chậm cảnh báo.</summary>
  private static readonly TimeSpan AddressTimeout = TimeSpan.FromSeconds(3);

  public async Task<List<AlertDelivery>> DispatchAsync(Alert alert, User user, IReadOnlyList<EmergencyContact> contacts, bool isRepeat, CancellationToken ct)
  {
    var address = await TryGetAddressAsync(alert.Lat, alert.Lng, ct);
    var deliveries = await notifier.NotifyAsync(alert.Id, contacts, AlertMessages.ForAlert(alert, user, isRepeat, address), ct);
    await uow.AlertDeliveryRepository.AddRangeAsync(deliveries, ct);
    return deliveries;
  }

  public async Task<List<AlertDelivery>> NotifySafeAsync(Alert alert, User user, IReadOnlyList<EmergencyContact> contacts, CancellationToken ct)
  {
    var deliveries = await notifier.NotifyAsync(alert.Id, contacts, AlertMessages.Safe(alert, user), ct);
    await uow.AlertDeliveryRepository.AddRangeAsync(deliveries, ct);
    return deliveries;
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
