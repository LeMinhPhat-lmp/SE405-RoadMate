using System.Globalization;
using RoadMate.Services.Maps;
using Microsoft.Extensions.Caching.Memory;
using RoadMate.Infrastructure.Weather;

namespace RoadMate.Services.Weather;

public class WeatherService(IWeatherClient client, IMemoryCache cache, IOptions<WeatherOptions> options, IUnitOfWork uow,
  TimeProvider clock, ILogger<WeatherService> log) : IWeatherService
{
  public async Task<Result<WeatherForecast>> GetForecastAsync(double lat, double lng, CancellationToken ct)
  {
    if (!Geo.ValidLatLng(lat, lng)) return Errors.InvalidLocation;
    var o = options.Value;
    var (gLat, gLng) = (Snap(lat, o.GridDegrees), Snap(lng, o.GridDegrees));
    var key = $"wx:{gLat.ToString("F2", CultureInfo.InvariantCulture)}:{gLng.ToString("F2", CultureInfo.InvariantCulture)}";

    return await cache.GetOrAddAsync(key, TimeSpan.FromMinutes(o.CacheMinutes), async () =>
    {
      var raw = await client.GetForecastAsync(gLat, gLng, ct);
      return raw.IsFailure ? raw.Error : WeatherParser.Parse(raw.Value, clock.UtcNow());
    });
  }

  public async Task<Result<WeatherDto>> GetAsync(double lat, double lng, CancellationToken ct)
  {
    var forecast = await GetForecastAsync(lat, lng, ct);
    return forecast.IsFailure ? forecast.Error : ToDto(lat, lng, forecast.Value);
  }

  public async Task<Result<TripWeatherDto>> GetForTripAsync(Guid userId, Guid tripId, CancellationToken ct)
  {
    var trip = await uow.TripRepository.GetOwnedWithDetailsAsync(tripId, userId, ct);
    if (trip is null) return Errors.TripNotFound;

    var points = new List<(string Name, string Kind, double Lat, double Lng)> { (trip.OriginName, "origin", trip.OriginLat, trip.OriginLng) };
    points.AddRange(trip.Waypoints.OrderBy(w => w.Seq).Select(w => (w.Name, "waypoint", w.Lat, w.Lng)));
    points.Add((trip.DestName, "destination", trip.DestLat, trip.DestLng));

    using var gate = new SemaphoreSlim(Math.Max(1, options.Value.MaxParallelRequests));
    var results = await Task.WhenAll(points.Select(async p =>
    {
      await gate.WaitAsync(ct);
      try { return (Point: p, Result: await GetAsync(p.Lat, p.Lng, ct)); }
      finally { gate.Release(); }
    }));

    var ok = results.Where(r => r.Result.IsSuccess).Select(r => new TripWeatherPointDto(r.Point.Name, r.Point.Kind, r.Result.Value)).ToList();
    var failed = results.Length - ok.Count;
    if (ok.Count == 0) return results[0].Result.Error;      // không điểm nào lấy được -> báo lỗi của nhà cung cấp
    if (failed > 0) log.LogWarning("Thời tiết chuyến {TripId}: {Failed}/{Total} điểm không lấy được", tripId, failed, results.Length);
    return new TripWeatherDto(trip.Id, ok, failed);
  }

  private WeatherDto ToDto(double lat, double lng, WeatherForecast f)
  {
    var o = options.Value;
    var now = clock.UtcNow();
    var from = new DateTime(now.Year, now.Month, now.Day, now.Hour, 0, 0, DateTimeKind.Utc);
    var hourly = f.Hourly.Where(h => h.Time >= from).Take(Math.Clamp(o.ForecastHours, 1, 48)).ToList();
    return new WeatherDto(lat, lng, f.Current, hourly, WeatherAssessor.Assess(f, o, now));
  }

  /// <summary>Làm tròn tới ô lưới gần nhất để nhiều điểm gần nhau dùng chung một dự báo.</summary>
  private static double Snap(double value, double grid) => grid <= 0 ? value : Math.Round(value / grid, MidpointRounding.AwayFromZero) * grid;
}
