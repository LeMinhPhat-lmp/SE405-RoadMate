using System.Text.Json;
using System.Text.RegularExpressions;
using RoadMate.Infrastructure.Maps;

namespace RoadMate.Services.Maps;

public partial class RoutingService(IGoongClient goong, IOptions<GoongOptions> options) : IRoutingService
{
  public const int MaxWaypoints = 20;
  public const int MaxMatrixOrigins = 10;
  public const int MaxMatrixDestinations = 25;

  // Goong Directions V2 KHÔNG có tham số tránh cao tốc/thu phí → xin nhiều tuyến (alternatives) và chọn tuyến
  // không có từ khoá tương ứng trong hướng dẫn rẽ. Đây là heuristic dựa trên chữ, nên luôn kèm cảnh báo khi không tránh được.
  [GeneratedRegex(@"cao\s*tốc|cao\s*toc", RegexOptions.IgnoreCase)] private static partial Regex HighwayRegex();
  [GeneratedRegex(@"thu\s*phí|thu\s*phi|\bBOT\b", RegexOptions.IgnoreCase)] private static partial Regex TollRegex();

  private sealed record RouteCandidate(List<(double Lat, double Lng)> Points, double DistanceM, int DurationS,
    List<StepDto> Steps, string? StartAddress, string? EndAddress, bool HasHighway, bool HasToll, List<string> ProviderWarnings);

  private sealed record LegResult(RouteCandidate Route, List<RouteWarningDto> Warnings);

  public async Task<Result<DirectionsResponse>> DirectionsAsync(DirectionsRequest req, CancellationToken ct)
  {
    if (req.Origin is null || req.Destination is null) return Error.Validation("MISSING_POINTS", "Thiếu điểm đi hoặc điểm đến.");
    var waypoints = req.Waypoints ?? [];
    if (waypoints.Count > MaxWaypoints) return Error.Validation("TOO_MANY_WAYPOINTS", $"Tối đa {MaxWaypoints} điểm dừng.");

    var pts = new List<LatLng> { req.Origin };
    pts.AddRange(waypoints);
    pts.Add(req.Destination);
    if (pts.Any(p => p is null || !Geo.ValidLatLng(p.Lat, p.Lng))) return Errors.InvalidLocation;

    var o = options.Value;
    var vehicle = VehicleMap.Resolve(req.Vehicle, o);
    if (vehicle.IsFailure) return vehicle.Error;

    // Các chặng độc lập nhau → chạy song song (giới hạn số request đồng thời để không dính rate limit của Goong).
    using var gate = new SemaphoreSlim(Math.Clamp(o.MaxParallelRequests, 1, 8));
    var tasks = Enumerable.Range(0, pts.Count - 1).Select(async i =>
    {
      await gate.WaitAsync(ct);
      try { return await RouteLegAsync(pts[i], pts[i + 1], vehicle.Value, req.AvoidHighway, req.AvoidToll, i, pts.Count - 2, ct); }
      finally { gate.Release(); }
    }).ToArray();
    var legs = await Task.WhenAll(tasks);
    var failed = legs.FirstOrDefault(l => l.IsFailure);
    if (failed is not null) return failed.Error;

    var all = new List<(double Lat, double Lng)>();
    var steps = new List<StepDto>();
    var legDtos = new List<LegDto>();
    var warnings = new List<RouteWarningDto>();
    double distM = 0; int durS = 0;
    foreach (var l in legs.Select(x => x.Value))
    {
      var r = l.Route;
      var decoded = r.Points.ToList();
      if (all.Count > 0 && decoded.Count > 0) decoded.RemoveAt(0);   // điểm đầu chặng sau trùng điểm cuối chặng trước
      all.AddRange(decoded);
      steps.AddRange(r.Steps);
      legDtos.Add(new LegDto(Math.Round(r.DistanceM / 1000, 2), (int)Math.Ceiling(r.DurationS / 60.0), r.StartAddress, r.EndAddress));
      warnings.AddRange(l.Warnings);
      distM += r.DistanceM; durS += r.DurationS;
    }

    BoundsDto? bounds = all.Count == 0 ? null : new BoundsDto(
      new LatLng(all.Max(p => p.Lat), all.Max(p => p.Lng)), new LatLng(all.Min(p => p.Lat), all.Min(p => p.Lng)));
    return new DirectionsResponse(PolylineCodec.Encode(all), Math.Round(distM / 1000, 2), (int)Math.Ceiling(durS / 60.0), steps, legDtos, bounds, warnings);
  }

  private async Task<Result<LegResult>> RouteLegAsync(LatLng a, LatLng b, string vehicle, bool avoidHighway, bool avoidToll, int legIndex, int lastLegIndex, CancellationToken ct)
  {
    var wantsAvoid = avoidHighway || avoidToll;
    var q = new GoongQuery().AddLatLng("origin", a.Lat, a.Lng).AddLatLng("destination", b.Lat, b.Lng)
      .Add("vehicle", vehicle).AddIf(wantsAvoid, "alternatives", "true");
    var r = await goong.GetAsync("v2/direction", q, ct);
    if (r.IsFailure) return r.Error;

    var candidates = r.Value.GetArray("routes").Select(route => ParseRoute(route, legIndex < lastLegIndex, legIndex + 1)).OfType<RouteCandidate>().ToList();
    if (candidates.Count == 0) return Error.NotFound("NO_ROUTE", "Không tìm được đường đi giữa các điểm.");

    var chosen = candidates[0];
    var warnings = new List<RouteWarningDto>();
    if (wantsAvoid)
    {
      var ok = candidates.FirstOrDefault(c => !(avoidHighway && c.HasHighway) && !(avoidToll && c.HasToll));
      if (ok is not null) chosen = ok;
      else
      {
        if (avoidHighway && chosen.HasHighway)
          warnings.Add(new("ROUTE_HAS_HIGHWAY", $"Chặng {legIndex + 1} có thể đi qua cao tốc (nhiều đoạn cấm xe máy), không tìm được tuyến thay thế."));
        if (avoidToll && chosen.HasToll)
          warnings.Add(new("ROUTE_HAS_TOLL", $"Chặng {legIndex + 1} có thể có trạm thu phí, không tìm được tuyến thay thế."));
      }
    }
    foreach (var w in chosen.ProviderWarnings) warnings.Add(new("PROVIDER_WARNING", w));
    return new LegResult(chosen, warnings);
  }

  private static RouteCandidate? ParseRoute(JsonElement route, bool intermediateLeg, int stopNumber)
  {
    var encoded = route.GetPath("overview_polyline")?.GetStringOrNull("points");
    if (string.IsNullOrEmpty(encoded)) return null;
    List<(double Lat, double Lng)> points;
    try { points = PolylineCodec.Decode(encoded); }
    catch (IndexOutOfRangeException) { return null; }   // polyline hỏng
    if (points.Count == 0) return null;

    double dist = 0; int dur = 0; string? startAddr = null, endAddr = null;
    var steps = new List<StepDto>();
    var text = new StringBuilder(route.GetStringOrNull("summary"));
    foreach (var leg in route.GetArray("legs"))
    {
      dist += leg.GetPath("distance")?.GetDoubleOrNull("value") ?? 0;
      dur += (int)(leg.GetPath("duration")?.GetDoubleOrNull("value") ?? 0);
      startAddr ??= leg.GetStringOrNull("start_address");
      endAddr = leg.GetStringOrNull("end_address") ?? endAddr;
      foreach (var st in leg.GetArray("steps"))
      {
        var instruction = TextHelper.StripHtmlTags(st.GetStringOrNull("html_instructions") ?? "").Normalize(NormalizationForm.FormC);
        text.Append(' ').Append(instruction);
        var start = st.GetOrNull("start_location");
        steps.Add(new StepDto(instruction, st.GetStringOrNull("maneuver"),
          (int)(st.GetPath("distance")?.GetDoubleOrNull("value") ?? 0), (int)(st.GetPath("duration")?.GetDoubleOrNull("value") ?? 0),
          start?.GetDoubleOrNull("lat"), start?.GetDoubleOrNull("lng")));
      }
    }
    // Chặng giữa: "Bạn đã đến điểm đích" thực chất là đến điểm dừng trung gian.
    if (intermediateLeg && steps.Count > 0 && steps[^1].Instruction.Contains("điểm đích", StringComparison.OrdinalIgnoreCase))
      steps[^1] = steps[^1] with { Instruction = $"Bạn đã đến điểm dừng {stopNumber}" };

    var warnings = route.GetOrNull("warnings") is { ValueKind: JsonValueKind.Array } w
      ? w.EnumerateArray().Where(x => x.ValueKind == JsonValueKind.String).Select(x => x.GetString()!).ToList() : [];
    var flat = text.ToString().Normalize(NormalizationForm.FormC);
    return new RouteCandidate(points, dist, dur, steps, startAddr, endAddr, HighwayRegex().IsMatch(flat), TollRegex().IsMatch(flat), warnings);
  }

  public async Task<Result<DistanceMatrixResponse>> MatrixAsync(DistanceMatrixRequest req, CancellationToken ct)
  {
    var origins = req.Origins ?? []; var dests = req.Destinations ?? [];
    if (origins.Count == 0 || dests.Count == 0) return Error.Validation("MISSING_POINTS", "Cần ít nhất 1 điểm xuất phát và 1 điểm đến.");
    if (origins.Count > MaxMatrixOrigins || dests.Count > MaxMatrixDestinations)
      return Error.Validation("TOO_MANY_POINTS", $"Tối đa {MaxMatrixOrigins} điểm xuất phát và {MaxMatrixDestinations} điểm đến.");
    if (origins.Concat(dests).Any(p => p is null || !Geo.ValidLatLng(p.Lat, p.Lng))) return Errors.InvalidLocation;
    var vehicle = VehicleMap.Resolve(req.Vehicle, options.Value);
    if (vehicle.IsFailure) return vehicle.Error;

    var q = new GoongQuery().AddLatLngList("origins", origins, '|').AddLatLngList("destinations", dests, '|').Add("vehicle", vehicle.Value);
    var r = await goong.GetAsync("v2/distancematrix", q, ct);
    if (r.IsFailure) return r.Error;

    var rows = r.Value.GetArray("rows").Select(row => row.GetArray("elements").Select(e => new MatrixElementDto(
      e.GetStringOrNull("status") ?? "UNKNOWN",
      e.GetPath("distance")?.GetDoubleOrNull("value") is { } d ? (int)Math.Round(d) : null,
      e.GetPath("duration")?.GetDoubleOrNull("value") is { } s ? (int)Math.Round(s) : null)).ToList()).ToList();
    if (rows.Count != origins.Count || rows.Any(x => x.Count != dests.Count))
      return Error.External("MAP_PROVIDER_ERROR", "Goong trả về ma trận không đúng kích thước yêu cầu.");
    return new DistanceMatrixResponse(rows);
  }
}
