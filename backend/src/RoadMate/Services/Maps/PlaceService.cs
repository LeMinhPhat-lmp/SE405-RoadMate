using System.Text.Json;
using RoadMate.Infrastructure.Maps;

namespace RoadMate.Services.Maps;

public class PlaceService(IGoongClient goong) : IPlaceService
{
  public async Task<Result<List<PlacePrediction>>> AutocompleteAsync(string q, double? lat, double? lng, CancellationToken ct)
  {
    if (string.IsNullOrWhiteSpace(q)) return Error.Validation("EMPTY_QUERY", "Thiếu từ khoá tìm kiếm.");
    var qs = $"input={Uri.EscapeDataString(q)}&limit=10";
    if (lat is not null && lng is not null) qs += $"&location={lat.Value.ToCoordinate()},{lng.Value.ToCoordinate()}";
    var r = await goong.GetAsync("Place/AutoComplete", qs, ct);
    if (r.IsFailure) return r.Error;
    var list = new List<PlacePrediction>();
    if (r.Value.TryGetProperty("predictions", out var preds))
      foreach (var p in preds.EnumerateArray())
      {
        string? main = null, sec = null;
        if (p.TryGetProperty("structured_formatting", out var sf)) { main = sf.GetStringOrNull("main_text"); sec = sf.GetStringOrNull("secondary_text"); }
        list.Add(new PlacePrediction(p.GetStringOrNull("place_id") ?? "", p.GetStringOrNull("description") ?? "", main, sec));
      }
    return list;
  }

  public async Task<Result<PlaceDetail>> DetailAsync(string placeId, CancellationToken ct)
  {
    var r = await goong.GetAsync("Place/Detail", $"place_id={Uri.EscapeDataString(placeId)}", ct);
    if (r.IsFailure) return r.Error;
    if (!r.Value.TryGetProperty("result", out var res) || res.ValueKind != JsonValueKind.Object)
      return Error.NotFound("PLACE_NOT_FOUND", "Không tìm thấy địa điểm.");
    var loc = res.GetProperty("geometry").GetProperty("location");
    return new PlaceDetail(placeId, res.GetStringOrNull("name") ?? "", res.GetStringOrNull("formatted_address"), loc.GetProperty("lat").GetDouble(), loc.GetProperty("lng").GetDouble());
  }

  public async Task<Result<ReverseResult>> ReverseAsync(double lat, double lng, CancellationToken ct)
  {
    if (!Geo.ValidLatLng(lat, lng)) return Errors.InvalidLocation;
    var r = await goong.GetAsync("Geocode", $"latlng={lat.ToCoordinate()},{lng.ToCoordinate()}", ct);
    if (r.IsFailure) return r.Error;
    if (!r.Value.TryGetProperty("results", out var rs) || rs.GetArrayLength() == 0)
      return Error.NotFound("ADDRESS_NOT_FOUND", "Không tìm thấy địa chỉ.");
    return new ReverseResult(rs[0].GetStringOrNull("place_id"), rs[0].GetStringOrNull("formatted_address") ?? "");
  }
}
