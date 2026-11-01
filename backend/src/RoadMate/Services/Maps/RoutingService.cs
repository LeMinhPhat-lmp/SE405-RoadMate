using RoadMate.Infrastructure.Maps;

namespace RoadMate.Services.Maps;

public class RoutingService(IGoongClient goong) : IRoutingService
{
  public async Task<Result<DirectionsResponse>> DirectionsAsync(DirectionsRequest req, CancellationToken ct)
  {
    var pts = new List<LatLng> { req.Origin };
    pts.AddRange(req.Waypoints ?? new List<LatLng>());
    pts.Add(req.Destination);
    if (pts.Count > 22) return Error.Validation("TOO_MANY_WAYPOINTS", "Tối đa 20 điểm dừng.");
    if (pts.Any(p => !Geo.ValidLatLng(p.Lat, p.Lng))) return Errors.InvalidLocation;

    // TODO: AvoidHighway/AvoidToll - kiểm tra Goong có hỗ trợ tham số tương ứng; hiện dùng vehicle=bike (đường xe máy) làm mặc định.
    var vehicle = string.IsNullOrWhiteSpace(req.Vehicle) ? "bike" : req.Vehicle;
    var all = new List<(double Lat, double Lng)>();
    double distM = 0; int durS = 0; var steps = new List<StepDto>();

    for (int i = 0; i < pts.Count - 1; i++)       // gọi từng chặng rồi nối polyline
    {
      var a = pts[i]; var b = pts[i + 1];
      var r = await goong.GetAsync("Direction", $"origin={a.Lat.ToCoordinate()},{a.Lng.ToCoordinate()}&destination={b.Lat.ToCoordinate()},{b.Lng.ToCoordinate()}&vehicle={vehicle}", ct);
      if (r.IsFailure) return r.Error;
      if (!r.Value.TryGetProperty("routes", out var routes) || routes.GetArrayLength() == 0)
        return Error.NotFound("NO_ROUTE", "Không tìm được đường đi giữa các điểm.");
      var route = routes[0];
      var decoded = PolylineCodec.Decode(route.GetProperty("overview_polyline").GetProperty("points").GetString() ?? "");
      if (all.Count > 0 && decoded.Count > 0) decoded.RemoveAt(0);
      all.AddRange(decoded);
      foreach (var leg in route.GetProperty("legs").EnumerateArray())
      {
        distM += leg.GetProperty("distance").GetProperty("value").GetDouble();
        durS += (int)leg.GetProperty("duration").GetProperty("value").GetDouble();
        foreach (var st in leg.GetProperty("steps").EnumerateArray())
          steps.Add(new StepDto(TextHelper.StripHtmlTags(st.GetStringOrNull("html_instructions") ?? ""), st.GetStringOrNull("maneuver"),
            (int)st.GetProperty("distance").GetProperty("value").GetDouble(), (int)st.GetProperty("duration").GetProperty("value").GetDouble()));
      }
    }
    return new DirectionsResponse(PolylineCodec.Encode(all), Math.Round(distM / 1000, 2), (int)Math.Ceiling(durS / 60.0), steps);
  }
}
