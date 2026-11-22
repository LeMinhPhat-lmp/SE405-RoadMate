using Microsoft.Extensions.Options;
using RoadMate.Application.Abstractions.Messaging;
using RoadMate.Application.Abstractions.Persistence;
using RoadMate.Application.Alerts.Interfaces;
using RoadMate.Domain.Alerts;

namespace RoadMate.Application.Alerts.Implements;

/// <summary>
/// At-least-once delivery with per-row retry (15 s / 30 s / 60 s, then Failed). A crash mid-send leaves the row Processing;
/// after the lease it is re-claimed. Rows of closed alerts are abandoned instead of sent (except the "safe" notice itself).
/// </summary>
public class OutboxProcessor(INotificationDeliveryRepository deliveries, IEnumerable<INotificationChannel> channels, IDeliveryMessageBuilder builder,
  IUnitOfWork uow, IOptions<AlertOptions> options, TimeProvider clock, ILogger<OutboxProcessor> log) : IOutboxProcessor
{
  public async Task<int> ProcessBatchAsync(string workerId, CancellationToken ct)
  {
    var o = options.Value;
    var batch = await deliveries.ClaimDueAsync(workerId, clock.GetUtcNow(), TimeSpan.FromSeconds(o.DeliveryLeaseSeconds), o.DeliveryBatchSize, ct);
    foreach (var d in batch)
    {
      try { await HandleAsync(d, o.Retry, ct); }
      catch (Exception ex) when (ex is not OperationCanceledException)
      {
        log.LogError(ex, "Gửi thông báo {DeliveryId} lỗi không mong đợi", d.Id);
        uow.DiscardChanges();
        await MarkFailedAfterErrorAsync(d.Id, ex.Message, o.Retry, ct);
      }
    }
    return batch.Count;
  }

  private async Task HandleAsync(NotificationDelivery d, DeliveryRetryPolicy retry, CancellationToken ct)
  {
    var now = clock.GetUtcNow();
    var ctx = await deliveries.LoadContextAsync(d, ct);
    if (ctx is null || (d.Kind != NotificationKind.Closed && !ctx.Alert.IsOpen))
    {
      d.Abandon(ctx is null ? "context-missing" : "alert-closed", now);
      await uow.SaveChangesAsync(ct);
      return;
    }

    var message = await builder.BuildAsync(d, ctx, ct);
    var channel = channels.FirstOrDefault(c => c.Channel == d.Channel);
    if (message is null || channel is null) { d.Abandon("no-recipient", now); await uow.SaveChangesAsync(ct); return; }

    var sent = await channel.SendAsync(message, ct);
    if (sent.IsSuccess)
    {
      d.MarkSent(sent.Value, clock.GetUtcNow());
      if (d.Kind != NotificationKind.Closed) ctx.Alert.MarkDelivered(clock.GetUtcNow());
    }
    else d.MarkFailed(sent.Error.Message, clock.GetUtcNow(), retry);
    await uow.SaveChangesAsync(ct);
  }

  private async Task MarkFailedAfterErrorAsync(Guid id, string error, DeliveryRetryPolicy retry, CancellationToken ct)
  {
    // After DiscardChanges the lease is still in the database; the row will be re-claimed when it expires, so recording is best-effort.
    log.LogWarning("Lượt gửi {DeliveryId} sẽ được thử lại khi hết hạn khoá: {Error}", id, error);
    await Task.CompletedTask;
  }
}
