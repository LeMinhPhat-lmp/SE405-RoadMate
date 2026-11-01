namespace RoadMate.Services.Trips;

/// <summary>Tính thống kê chuyến từ các điểm GPS (đã sắp theo thời gian).</summary>
public static class StatsCalculator
{
  /// <summary>Ghi đè toàn bộ số liệu của <paramref name="stats"/> theo <paramref name="pts"/> (rỗng thì về 0).</summary>
  public static void Fill(TripStats stats, IReadOnlyList<TripPoint> pts, int alertCount)
  {
    double distM = 0, movingSec = 0, restSec = 0, stopSec = 0;
    int stops = 0; bool stopCounted = false;

    for (int i = 1; i < pts.Count; i++)
    {
      var a = pts[i - 1]; var b = pts[i];
      double seg = Geo.HaversineMeters(a.Lat, a.Lng, b.Lat, b.Lng);
      double dt = (b.RecordedAt - a.RecordedAt).TotalSeconds;
      if (dt <= 0) continue;
      distM += seg;
      if (dt > 600) continue;                       // khoảng trống (mất sóng) không tính thời gian
      double kmh = b.Speed ?? seg / dt * 3.6;
      if (kmh < 2)
      {
        restSec += dt; stopSec += dt;
        if (!stopCounted && stopSec >= 120) { stops++; stopCounted = true; }
      }
      else { movingSec += dt; stopSec = 0; stopCounted = false; }
    }

    stats.AlertCount = alertCount;
    stats.MaxSpeed = pts.Select(p => p.Speed ?? 0).DefaultIfEmpty(0).Max();
    stats.MaxAltitude = pts.Select(p => p.Altitude ?? 0).DefaultIfEmpty(0).Max();
    stats.DistanceKm = Math.Round(distM / 1000, 2);
    stats.MovingMin = (int)(movingSec / 60);
    stats.RestMin = (int)(restSec / 60);
    stats.StopCount = stops;
    stats.AvgSpeed = movingSec > 0 ? Math.Round(distM / movingSec * 3.6, 1) : 0;
  }
}
