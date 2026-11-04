using System.Globalization;
using Microsoft.Extensions.Caching.Memory;
using RoadMate.Infrastructure.Notifications;
using RoadMate.Infrastructure.Weather;

namespace RoadMate.Services.Weather;

/// <summary>
/// Mỗi chu kỳ: với mỗi chuyến đang chạy có vị trí, chọn tối đa WatchMaxPoints điểm phía trước (điểm dừng + điểm đến) gần nhất trong
/// WatchLookaheadKm, nếu dự báo xấu thì gửi MỘT push cho chủ chuyến. Chống nhắc lặp bằng cache trong bộ nhớ theo (chuyến, ô lưới):
/// khởi động lại server thì cooldown reset (chấp nhận, đã giả định một instance).
/// </summary>
public class WeatherWatchService(IUnitOfWork uow, IWeatherService weather, IPushSender push, IMemoryCache cache,
  IOptions<WeatherOptions> options, TimeProvider clock, ILogger<WeatherWatchService> log) : IWeatherWatchService
{
  public async Task RunOnceAsync(CancellationToken ct)
  {
    foreach (var tripId in await uow.TripRepository.ListActiveIdsAsync(ct))
    {
      try
      {
        var trip = await uow.TripRepository.GetActiveForMonitoringAsync(tripId, ct);
        if (trip is not null) await CheckTripAsync(trip, ct);
      }
      catch (Exception ex) when (ex is not OperationCanceledException)
      {
        log.LogError(ex, "Theo dõi thời tiết chuyến {TripId} lỗi, bỏ qua", tripId);
        uow.DiscardChanges();
      }
    }
  }

  private async Task CheckTripAsync(Trip trip, CancellationToken ct)
  {
    if (trip.Live is not { } live) return;
    var o = options.Value;
    var now = clock.UtcNow();

    var ahead = trip.Waypoints.Select(w => (Name: w.Name, Lat: w.Lat, Lng: w.Lng))
      .Append((Name: trip.DestName, Lat: trip.DestLat, Lng: trip.DestLng))
      .Select(p => (p.Name, p.Lat, p.Lng, DistanceM: Geo.HaversineMeters(live.Lat, live.Lng, p.Lat, p.Lng)))
      .Where(p => p.DistanceM > o.WatchArrivedMeters && p.DistanceM <= o.WatchLookaheadKm * 1000)
      .OrderBy(p => p.DistanceM).Take(o.WatchMaxPoints).ToList();

    var lines = new List<string>();
    var codes = new HashSet<string>();
    foreach (var p in ahead)
    {
      var forecast = await weather.GetForecastAsync(p.Lat, p.Lng, ct);
      if (forecast.IsFailure) { log.LogDebug("Bỏ qua điểm {Point} của chuyến {TripId}: {Code}", p.Name, trip.Id, forecast.Error.Code); continue; }

      var warnings = WeatherAssessor.Assess(forecast.Value, o, now);
      if (warnings.Count == 0) continue;
      var cooldownKey = $"wx-warn:{trip.Id}:{Math.Round(p.Lat, 1).ToString("F1", CultureInfo.InvariantCulture)}:{Math.Round(p.Lng, 1).ToString("F1", CultureInfo.InvariantCulture)}";
      if (cache.TryGetValue(cooldownKey, out _)) continue;

      cache.Set(cooldownKey, true, TimeSpan.FromMinutes(o.NotifyCooldownMinutes));
      lines.Add($"Gần {p.Name}: {string.Join("; ", warnings.Select(w => $"{w.Message.ToLowerInvariant()} (khoảng {w.At.AddHours(7):HH:mm})"))}");
      foreach (var w in warnings) codes.Add(w.Code);
    }
    if (lines.Count == 0) return;

    var token = trip.Owner?.FcmToken;
    if (string.IsNullOrEmpty(token))
    {
      log.LogWarning("Chuyến {TripId} có thời tiết xấu phía trước nhưng chủ chuyến chưa có FCM token", trip.Id);
      return;
    }
    var data = new Dictionary<string, string> { ["type"] = "weather_warning", ["tripId"] = trip.Id.ToString(), ["codes"] = string.Join(",", codes) };
    var body = string.Join(" ", lines) + " Hãy cân nhắc dừng nghỉ hoặc đổi tuyến.";
    var r = await push.SendAsync(token, "RoadMate: thời tiết xấu phía trước", body, data, ct);
    log.LogInformation("Cảnh báo thời tiết cho chuyến {TripId} ({Codes}): {Result}", trip.Id, string.Join(",", codes), r.IsSuccess ? "đã gửi push" : r.Error.Code);
  }
}
