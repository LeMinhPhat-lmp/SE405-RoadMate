using RoadMate.Infrastructure.Notifications;

namespace RoadMate.Services.Monitoring;

public class TripMonitorService(IUnitOfWork uow, IAlertService alerts, IPushSender push, IOptions<SafetyOptions> options,
  TimeProvider clock, ILogger<TripMonitorService> log) : ITripMonitorService
{
  public async Task RunOnceAsync(CancellationToken ct)
  {
    var now = clock.UtcNow();
    foreach (var tripId in await uow.TripRepository.ListActiveIdsAsync(ct))
    {
      try
      {
        var trip = await uow.TripRepository.GetActiveForMonitoringAsync(tripId, ct);
        if (trip is null) continue;
        await CheckLostSignalAsync(trip, now, ct);
        await CheckLateReturnAsync(trip, now, ct);
      }
      catch (Exception ex) when (ex is not OperationCanceledException)
      {
        log.LogError(ex, "Giám sát chuyến {TripId} lỗi, bỏ qua và tiếp tục chuyến khác", tripId);
        uow.DiscardChanges();
      }
    }
  }

  /// <summary>Không có heartbeat/GPS quá ngưỡng -> lostSignal (tự đóng khi dữ liệu trở lại, xem TelemetryService).</summary>
  private async Task CheckLostSignalAsync(Trip trip, DateTime now, CancellationToken ct)
  {
    var lastSeen = trip.Live?.UpdatedAt ?? trip.StartTime ?? now;
    if (now - lastSeen < TimeSpan.FromMinutes(options.Value.LostSignalMinutes)) return;
    if (await uow.AlertRepository.HasOpenAsync(trip.Id, AlertType.LostSignal, ct)) return;

    var r = await alerts.RaiseSystemAsync(trip.OwnerId, trip.Id, AlertType.LostSignal,
      trip.Live?.Lat ?? trip.OriginLat, trip.Live?.Lng ?? trip.OriginLng, trip.Live?.Battery, ct);
    log.LogWarning("Chuyến {TripId} mất tín hiệu từ {LastSeen:o}: {Result}", trip.Id, lastSeen, r.IsSuccess ? "đã tạo cảnh báo" : r.Error.Code);
  }

  /// <summary>Quá giờ về + 60 phút: nhắc chủ chuyến trước; sau 15 phút không phản hồi mới SMS người thân.</summary>
  private async Task CheckLateReturnAsync(Trip trip, DateTime now, CancellationToken ct)
  {
    if (trip.ExpectedReturnTime is not { } expected) return;
    if (now < expected.AddMinutes(options.Value.LateReturnGraceMinutes)) return;

    if (trip.LateReturnRemindedAt is not { } remindedAt)
    {
      await RemindOwnerAsync(trip, ct);
      trip.LateReturnRemindedAt = now;
      await uow.SaveChangesAsync(ct);
      return;
    }
    if (now - remindedAt < TimeSpan.FromMinutes(options.Value.LateReturnOwnerResponseMinutes)) return;
    if (await uow.AlertRepository.ExistsSinceAsync(trip.Id, AlertType.LateReturn, remindedAt, ct)) return;

    var r = await alerts.RaiseSystemAsync(trip.OwnerId, trip.Id, AlertType.LateReturn,
      trip.Live?.Lat ?? trip.OriginLat, trip.Live?.Lng ?? trip.OriginLng, trip.Live?.Battery, ct);
    log.LogWarning("Chuyến {TripId} về trễ và chủ chuyến không phản hồi: {Result}", trip.Id, r.IsSuccess ? "đã báo người thân" : r.Error.Code);
  }

  private async Task RemindOwnerAsync(Trip trip, CancellationToken ct)
  {
    var token = trip.Owner?.FcmToken;
    if (string.IsNullOrEmpty(token))
    {
      log.LogWarning("Chuyến {TripId} quá giờ về nhưng chủ chuyến chưa có FCM token để nhắc", trip.Id);
      return;
    }
    var data = new Dictionary<string, string> { ["type"] = "late_return_reminder", ["tripId"] = trip.Id.ToString() };
    var r = await push.SendAsync(token, "RoadMate: bạn đã quá giờ dự kiến về",
      "Bạn có ổn không? Hãy mở app để cập nhật giờ về hoặc kết thúc chuyến, nếu không người thân sẽ được báo.", data, ct);
    log.LogInformation("Nhắc chủ chuyến {TripId} về trễ: {Result}", trip.Id, r.IsSuccess ? "đã gửi push" : r.Error.Code);
  }
}
