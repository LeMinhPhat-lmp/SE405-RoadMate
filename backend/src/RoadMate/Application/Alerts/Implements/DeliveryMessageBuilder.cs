using Microsoft.Extensions.Options;
using RoadMate.Application.Abstractions.Messaging;
using RoadMate.Application.Abstractions.Persistence;
using RoadMate.Application.Alerts.Interfaces;
using RoadMate.Application.Common;
using RoadMate.Application.Maps;
using RoadMate.Application.Maps.Interfaces;
using RoadMate.Application.Tracking;
using RoadMate.Domain.Alerts;

namespace RoadMate.Application.Alerts.Implements;

public class DeliveryMessageBuilder(IAlertPolicyResolver policies, IGeocodingService geocoding, IOptions<TrackingOptions> tracking, TimeProvider clock) : IDeliveryMessageBuilder
{
  /// <summary>The address is a convenience: past this time the SMS goes without it instead of delaying the alert.</summary>
  private static readonly TimeSpan AddressTimeout = TimeSpan.FromSeconds(3);

  public async Task<NotificationMessage?> BuildAsync(NotificationDelivery d, DeliveryContext ctx, CancellationToken ct)
  {
    var recipient = d.Channel == DeliveryChannel.Push ? ctx.PushToken : ctx.Phone;
    if (string.IsNullOrEmpty(recipient)) return null;

    NotificationContent content;
    if (d.Kind == NotificationKind.Closed) content = AlertMessageComposer.Closed(ctx.Alert, ctx.Owner.Name);
    else
    {
      var address = await TryGetAddressAsync(ctx.Alert.Lat, ctx.Alert.Lng, ct);
      var ackUrl = ctx.AckToken is null ? null : TrackingUrl.Build(tracking.Value.AckUrl, ctx.AckToken);
      content = AlertMessageComposer.ForAlert(ctx.Alert, policies.For(ctx.Alert.Type), new OwnerInfo(ctx.Owner.Name, ctx.Owner.BloodType, ctx.Owner.PlateNumber),
        d.Kind == NotificationKind.Repeat, clock.GetUtcNow(), address, ackUrl);
    }
    return new NotificationMessage(recipient, content.PushTitle, content.Sms, content.Data);
  }

  /// <summary>Any failure (no key, quota, timeout) returns null: the alert must still go out.</summary>
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
