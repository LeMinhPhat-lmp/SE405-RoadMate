using System.Text.Json;
using System.Text.RegularExpressions;

namespace RoadMate.Services;

public interface IMapService
{
  Task<Result<DirectionsResponse>> DirectionsAsync(DirectionsRequest req, CancellationToken ct);
  Task<Result<List<PlacePrediction>>> AutocompleteAsync(string q, double? lat, double? lng, CancellationToken ct);
  Task<Result<PlaceDetail>> PlaceDetailAsync(string placeId, CancellationToken ct);
  Task<Result<ReverseResult>> ReverseAsync(double lat, double lng, CancellationToken ct);
}

/// <summary>Proxy Goong Maps (giấu API key phía server). Định dạng JSON của Goong tương tự Google Maps.</summary>
public class MapService : IMapService
{
  private readonly HttpClient _http; private readonly GoongOptions _o;
  public MapService(HttpClient http, IOptions<GoongOptions> o) { _http = http; _o = o.Value; }

  private static string F(double d) => d.ToString("F6", CultureInfo.InvariantCulture);
  private static string? Str(JsonElement e, string name) => e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;

  private async Task<Result<JsonElement>> GetAsync(string path, string query, CancellationToken ct)
  {
    if (string.IsNullOrWhiteSpace(_o.ApiKey)) return Error.Failure("MAP_NOT_CONFIGURED", "Chưa cấu hình Goong:ApiKey.");
    try
    {
      var url = $"{_o.BaseUrl.TrimEnd('/')}/{path}?{query}&api_key={Uri.EscapeDataString(_o.ApiKey)}";
      using var res = await _http.GetAsync(url, ct);
      if (!res.IsSuccessStatusCode) return Error.External("MAP_PROVIDER_ERROR", $"Goong trả về HTTP {(int)res.StatusCode}.");
      using var doc = await JsonDocument.ParseAsync(await res.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
      return doc.RootElement.Clone();
    }
    catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
    {
      return Error.External("MAP_PROVIDER_ERROR", ex.Message);
    }
  }

  public async Task<Result<DirectionsResponse>> DirectionsAsync(DirectionsRequest req, CancellationToken ct)
  {
    var pts = new List<LatLng> { req.Origin };
    pts.AddRange(req.Waypoints ?? new List<LatLng>());
    pts.Add(req.Destination);
    if (pts.Count > 22) return Error.Validation("TOO_MANY_WAYPOINTS", "Tối đa 20 điểm dừng.");
    if (pts.Any(p => !Geo.ValidLatLng(p.Lat, p.Lng))) return Error.Validation("INVALID_LOCATION", "Toạ độ không hợp lệ.");

    // TODO: AvoidHighway/AvoidToll - kiểm tra Goong có hỗ trợ tham số tương ứng; hiện dùng vehicle=bike (đường xe máy) làm mặc định.
    var vehicle = string.IsNullOrWhiteSpace(req.Vehicle) ? "bike" : req.Vehicle;
    var all = new List<(double Lat, double Lng)>();
    double distM = 0; int durS = 0; var steps = new List<StepDto>();

    for (int i = 0; i < pts.Count - 1; i++)       // gọi từng chặng rồi nối polyline
    {
      var a = pts[i]; var b = pts[i + 1];
      var r = await GetAsync("Direction", $"origin={F(a.Lat)},{F(a.Lng)}&destination={F(b.Lat)},{F(b.Lng)}&vehicle={vehicle}", ct);
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
          steps.Add(new StepDto(Regex.Replace(Str(st, "html_instructions") ?? "", "<.*?>", ""), Str(st, "maneuver"),
            (int)st.GetProperty("distance").GetProperty("value").GetDouble(), (int)st.GetProperty("duration").GetProperty("value").GetDouble()));
      }
    }
    return new DirectionsResponse(PolylineCodec.Encode(all), Math.Round(distM / 1000, 2), (int)Math.Ceiling(durS / 60.0), steps);
  }

  public async Task<Result<List<PlacePrediction>>> AutocompleteAsync(string q, double? lat, double? lng, CancellationToken ct)
  {
    if (string.IsNullOrWhiteSpace(q)) return Error.Validation("EMPTY_QUERY", "Thiếu từ khoá tìm kiếm.");
    var qs = $"input={Uri.EscapeDataString(q)}&limit=10";
    if (lat is not null && lng is not null) qs += $"&location={F(lat.Value)},{F(lng.Value)}";
    var r = await GetAsync("Place/AutoComplete", qs, ct);
    if (r.IsFailure) return r.Error;
    var list = new List<PlacePrediction>();
    if (r.Value.TryGetProperty("predictions", out var preds))
      foreach (var p in preds.EnumerateArray())
      {
        string? main = null, sec = null;
        if (p.TryGetProperty("structured_formatting", out var sf)) { main = Str(sf, "main_text"); sec = Str(sf, "secondary_text"); }
        list.Add(new PlacePrediction(Str(p, "place_id") ?? "", Str(p, "description") ?? "", main, sec));
      }
    return list;
  }

  public async Task<Result<PlaceDetail>> PlaceDetailAsync(string placeId, CancellationToken ct)
  {
    var r = await GetAsync("Place/Detail", $"place_id={Uri.EscapeDataString(placeId)}", ct);
    if (r.IsFailure) return r.Error;
    if (!r.Value.TryGetProperty("result", out var res) || res.ValueKind != JsonValueKind.Object)
      return Error.NotFound("PLACE_NOT_FOUND", "Không tìm thấy địa điểm.");
    var loc = res.GetProperty("geometry").GetProperty("location");
    return new PlaceDetail(placeId, Str(res, "name") ?? "", Str(res, "formatted_address"), loc.GetProperty("lat").GetDouble(), loc.GetProperty("lng").GetDouble());
  }

  public async Task<Result<ReverseResult>> ReverseAsync(double lat, double lng, CancellationToken ct)
  {
    if (!Geo.ValidLatLng(lat, lng)) return Error.Validation("INVALID_LOCATION", "Toạ độ không hợp lệ.");
    var r = await GetAsync("Geocode", $"latlng={F(lat)},{F(lng)}", ct);
    if (r.IsFailure) return r.Error;
    if (!r.Value.TryGetProperty("results", out var rs) || rs.GetArrayLength() == 0)
      return Error.NotFound("ADDRESS_NOT_FOUND", "Không tìm thấy địa chỉ.");
    return new ReverseResult(Str(rs[0], "place_id"), Str(rs[0], "formatted_address") ?? "");
  }
}
