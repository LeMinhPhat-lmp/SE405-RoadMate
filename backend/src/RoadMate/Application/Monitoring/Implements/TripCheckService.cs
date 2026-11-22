using Microsoft.Extensions.Options;
using RoadMate.Application.Abstractions.Messaging;
using RoadMate.Application.Abstractions.Persistence;
using RoadMate.Application.Alerts;
using RoadMate.Application.Alerts.Interfaces;
using RoadMate.Application.Monitoring.Interfaces;
using RoadMate.Domain.Alerts;
using RoadMate.Domain.Common;
using RoadMate.Domain.Trips;
using RoadMate.Domain.ValueObjects;

namespace RoadMate.Application.Monitoring.Implements;

public class TripCheckService(ITripRepository trips, IAlertRepository alerts, IAlertRaiser raiser, IPushNotifier push, IUnitOfWork uow,
  IOptions<SafetyOptions> options, TimeProvider clock, ILogger<TripCheckService> log) : ITripCheckService
{
  public async Task CheckAsync(Guid tripId, CancellationToken ct)
  {
    var trip = (await trips.ListActiveForMonitoringAsync(ct)).FirstOrDefault(t => t.Id == tripId);
    if (trip is null) return;
    var now = clock.GetUtcNow();
    await CheckLostSignalAsync(trip, now, ct);
    await CheckLateReturnAsync(trip, now, ct);
  }

  /// <summary>No heartbeat/GPS for too long → lostSignal alert (auto-closed when data returns: LocationReported handler).</summary>
  private async Task CheckLostSignalAsync(Trip trip, DateTimeOffset now, CancellationToken ct)
  {
    var lastSeen = trip.Live?.UpdatedAt ?? trip.StartTime ?? now;
    if (now - lastSeen < TimeSpan.FromMinutes(options.Value.LostSignalMinutes)) return;
    if (trip.LostSignalAlertOpen || await alerts.HasOpenAsync(trip.Id, AlertType.LostSignal, ct)) return;
    var r = await RaiseAsync(trip, AlertType.LostSignal, now, ct);
    if (r) { trip.MarkLostSignalRaised(now); await uow.SaveChangesAsync(ct); }
    log.LogWarning("Chuyến {TripId} mất tín hiệu từ {LastSeen:o}: {Result}", trip.Id, lastSeen, r ? "đã tạo cảnh báo" : "không tạo được");
  }

  /// <summary>Past expected return + grace: remind the owner first; only after the response window SMS the relatives.</summary>
  private async Task CheckLateReturnAsync(Trip trip, DateTimeOffset now, CancellationToken ct)
  {
    if (trip.ExpectedReturnTime is not { } expected) return;
    if (now < expected.AddMinutes(options.Value.LateReturnGraceMinutes)) return;
    if (trip.LateReturnRemindedAt is not { } remindedAt)
    {
      await RemindOwnerAsync(trip, ct);
      trip.MarkLateReturnReminded(now);
      await uow.SaveChangesAsync(ct);
      return;
    }
    if (now - remindedAt < TimeSpan.FromMinutes(options.Value.LateReturnOwnerResponseMinutes)) return;
    if (await alerts.ExistsSinceAsync(trip.Id, AlertType.LateReturn, remindedAt, ct)) return;
    var r = await RaiseAsync(trip, AlertType.LateReturn, now, ct);
    log.LogWarning("Chuyến {TripId} về trễ và chủ chuyến không phản hồi: {Result}", trip.Id, r ? "đã báo người thân" : "không tạo được");
  }

  private async Task<bool> RaiseAsync(Trip trip, AlertType type, DateTimeOffset now, CancellationToken ct)
  {
    var at = trip.Live is { } l ? GeoPoint.Create(l.Lat, l.Lng) : GeoPoint.Create(trip.OriginLat, trip.OriginLng);
    if (at.IsFailure) return false;
    var cmd = new RaiseAlertCommand(trip.Id, trip.OwnerId, ClientRequestId.Create($"sys-{EnumWire.ToWire(type)}-{Guid.NewGuid():N}").Value, type, at.Value, trip.Live?.Battery, null);
    return (await raiser.RaiseAsync(cmd, now, ct)).IsSuccess;
  }

  private async Task RemindOwnerAsync(Trip trip, CancellationToken ct)
  {
    var sent = await push.NotifyUsersAsync([trip.OwnerId], new PushContent("RoadMate: bạn đã quá giờ dự kiến về",
      "Bạn có ổn không? Hãy mở app để cập nhật giờ về hoặc kết thúc chuyến, nếu không người thân sẽ được báo.",
      new Dictionary<string, string> { ["type"] = "late_return_reminder", ["tripId"] = trip.Id.ToString() }), ct);
    if (sent == 0) log.LogWarning("Chuyến {TripId} quá giờ về nhưng không nhắc được chủ chuyến (chưa có FCM token hoặc gửi lỗi)", trip.Id);
  }
}
