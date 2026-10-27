namespace RoadMate.Common;

/// <summary>Tính thống kê chuyến từ các điểm GPS (đã sắp theo thời gian).</summary>
public static class StatsCalculator
{
  public static TripStats Compute(Guid tripId, IReadOnlyList<TripPoint> pts, int alertCount)
  {
    var s = new TripStats { TripId = tripId, AlertCount = alertCount };
    if (pts.Count == 0) return s;

    double distM = 0, movingSec = 0, restSec = 0, stopSec = 0;
    int stops = 0; bool stopCounted = false;
    s.MaxSpeed = pts.Max(p => p.Speed ?? 0);
    s.MaxAltitude = pts.Max(p => p.Altitude ?? 0);

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
    s.DistanceKm = Math.Round(distM / 1000, 2);
    s.MovingMin = (int)(movingSec / 60);
    s.RestMin = (int)(restSec / 60);
    s.StopCount = stops;
    s.AvgSpeed = movingSec > 0 ? Math.Round(distM / movingSec * 3.6, 1) : 0;
    return s;
  }
}
