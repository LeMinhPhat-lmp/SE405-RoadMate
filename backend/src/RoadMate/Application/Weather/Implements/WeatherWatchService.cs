using System.Globalization;
using Microsoft.Extensions.Options;
using RoadMate.Application.Abstractions.Messaging;
using RoadMate.Application.Abstractions.Persistence;
using RoadMate.Application.Weather.Interfaces;
using RoadMate.Domain.Common;
using RoadMate.Domain.Trips;

namespace RoadMate.Application.Weather.Implements;

/// <summary>
/// Each cycle, for every active trip with a live position: pick the nearest ≤ WatchMaxPoints stops ahead within WatchLookaheadKm;
/// when the forecast is bad send ONE push to the owner. Repeats are suppressed by an in-memory cooldown per (trip, grid cell).
/// </summary>
public class WeatherWatchService(ITripRepository trips, IWeatherService weather, IPushNotifier push, ICooldown cooldown,
  IOptions<WeatherOptions> options, TimeProvider clock, ILogger<WeatherWatchService> log) : IWeatherWatchService
{
  public async Task RunOnceAsync(CancellationToken ct)
  {
    foreach (var trip in await trips.ListActiveForMonitoringAsync(ct))
    {
      try { await CheckTripAsync(trip, ct); }
      catch (Exception ex) when (ex is not OperationCanceledException) { log.LogError(ex, "Theo dõi thời tiết chuyến {TripId} lỗi, bỏ qua", trip.Id); }
    }
  }

  private async Task CheckTripAsync(Trip trip, CancellationToken ct)
  {
    if (trip.Live is not { } live) return;
    var o = options.Value;
    var now = clock.GetUtcNow().UtcDateTime;
    var ahead = trip.Waypoints.Select(w => (w.Name, w.Lat, w.Lng))
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
      var key = $"wx-warn:{trip.Id}:{Math.Round(p.Lat, 1).ToString("F1", CultureInfo.InvariantCulture)}:{Math.Round(p.Lng, 1).ToString("F1", CultureInfo.InvariantCulture)}";
      if (!cooldown.TryAcquire(key, TimeSpan.FromMinutes(o.NotifyCooldownMinutes))) continue;
      lines.Add($"Gần {p.Name}: {string.Join("; ", warnings.Select(w => $"{w.Message.ToLowerInvariant()} (khoảng {w.At.AddHours(7):HH:mm})"))}");
      foreach (var w in warnings) codes.Add(w.Code);
    }
    if (lines.Count == 0) return;

    var sent = await push.NotifyUsersAsync([trip.OwnerId], new PushContent("RoadMate: thời tiết xấu phía trước",
      string.Join(" ", lines) + " Hãy cân nhắc dừng nghỉ hoặc đổi tuyến.",
      new Dictionary<string, string> { ["type"] = "weather_warning", ["tripId"] = trip.Id.ToString(), ["codes"] = string.Join(",", codes) }), ct);
    log.LogInformation("Cảnh báo thời tiết cho chuyến {TripId} ({Codes}): đã push {Sent}", trip.Id, string.Join(",", codes), sent);
  }
}
