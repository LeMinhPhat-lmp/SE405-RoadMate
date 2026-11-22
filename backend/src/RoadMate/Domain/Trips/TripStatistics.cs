using RoadMate.Domain.Common;

namespace RoadMate.Domain.Trips;

/// <summary>
/// Pure statistics over GPS points sorted by time. Rules: a segment with dt ≤ 0 is skipped; a segment with dt &gt; 600 s (signal gap)
/// counts in NEITHER distance nor time (fix of the legacy inflated average speed); rest = speed &lt; 2 km/h; a stop = ≥ 120 s of rest.
/// </summary>
public static class TripStatistics
{
  public const double GapSeconds = 600, RestKmh = 2, StopSeconds = 120;

  public static TripStatisticsValues Compute(IReadOnlyList<TripPointSample> pts, int alertCount)
  {
    double distM = 0, movingDistM = 0, movingSec = 0, restSec = 0, stopSec = 0;
    var stops = 0; var stopCounted = false;

    for (var i = 1; i < pts.Count; i++)
    {
      var a = pts[i - 1]; var b = pts[i];
      var seg = Geo.HaversineMeters(a.Lat, a.Lng, b.Lat, b.Lng);
      var dt = (b.RecordedAt - a.RecordedAt).TotalSeconds;
      if (dt <= 0 || dt > GapSeconds) continue;
      distM += seg;
      var kmh = b.Speed ?? seg / dt * 3.6;
      if (kmh < RestKmh)
      {
        restSec += dt; stopSec += dt;
        if (!stopCounted && stopSec >= StopSeconds) { stops++; stopCounted = true; }
      }
      else { movingSec += dt; movingDistM += seg; stopSec = 0; stopCounted = false; }
    }

    return new TripStatisticsValues(
      Math.Round(distM / 1000, 2), (int)(movingSec / 60), (int)(restSec / 60), stops,
      pts.Select(p => p.Speed ?? 0).DefaultIfEmpty(0).Max(), movingSec > 0 ? Math.Round(movingDistM / movingSec * 3.6, 1) : 0,
      pts.Select(p => p.Altitude ?? 0).DefaultIfEmpty(0).Max(), alertCount);
  }
}
