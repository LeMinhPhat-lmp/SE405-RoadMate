using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using RoadMate.Infrastructure.Maps;

namespace RoadMate.Services.Maps;

public class GeocodingService(IGoongClient goong, IMemoryCache cache, IOptions<GoongOptions> options) : IGeocodingService
{
  public async Task<Result<ReverseResult>> ReverseAsync(double lat, double lng, CancellationToken ct)
  {
    if (!Geo.ValidLatLng(lat, lng)) return Errors.InvalidLocation;
    var o = options.Value;
    return await cache.GetOrAddAsync<ReverseResult>($"rev:{lat:F4}:{lng:F4}", TimeSpan.FromMinutes(o.ReverseCacheMinutes), async () =>
    {
      var q = new GoongQuery().AddLatLng("latlng", lat, lng).Add("limit", 1)
        .AddIf(o.IncludeDeprecatedAdministrativeUnit, "has_deprecated_administrative_unit", "true");
      var r = await goong.GetAsync("v2/geocode", q, ct);
      if (r.IsFailure) return r.Error;
      var first = r.Value.GetArray("results").Cast<JsonElement?>().FirstOrDefault();
      if (first is not { } f) return Error.NotFound("ADDRESS_NOT_FOUND", "Không tìm thấy địa chỉ.");
      var g = Parse(f);
      return Result.Success(new ReverseResult(g.PlaceId, g.Address, g.Name, g.Lat, g.Lng, g.Commune, g.Province, g.Types, g.DeprecatedAddress));
    });
  }

  public async Task<Result<List<GeocodeResult>>> ForwardAsync(string address, int? limit, CancellationToken ct)
  {
    address = (address ?? "").Trim();
    if (address.Length == 0) return Error.Validation("EMPTY_ADDRESS", "Thiếu địa chỉ cần tìm.");
    if (address.Length > PlaceService.MaxQueryLength) return Error.Validation("ADDRESS_TOO_LONG", $"Địa chỉ tối đa {PlaceService.MaxQueryLength} ký tự.");
    var n = Math.Clamp(limit ?? 5, 1, PlaceService.MaxLimit);
    var o = options.Value;
    var q = new GoongQuery().Add("address", address).Add("limit", n)
      .AddIf(o.IncludeDeprecatedAdministrativeUnit, "has_deprecated_administrative_unit", "true");
    var r = await goong.GetAsync("v2/geocode", q, ct);
    if (r.IsFailure) return r.Error;
    // Doc V2 không nêu rõ `limit` có áp dụng cho forward geocode → tự cắt để đảm bảo hợp đồng của API mình.
    return r.Value.GetArray("results").Select(Parse).Where(g => g.PlaceId.Length > 0).Take(n).ToList();
  }

  public async Task<Result<StreetResult>> StreetAsync(double lat, double lng, CancellationToken ct)
  {
    if (!Geo.ValidLatLng(lat, lng)) return Errors.InvalidLocation;
    var o = options.Value;
    var q = new GoongQuery().AddLatLng("latlng", lat, lng).AddIf(o.IncludeDeprecatedAdministrativeUnit, "has_deprecated_administrative_unit", "true");
    var r = await goong.GetAsync("v2/geocode/street", q, ct);
    if (r.IsFailure) return r.Error;
    var first = r.Value.GetArray("results").Cast<JsonElement?>().FirstOrDefault();
    if (first is not { } f) return Error.NotFound("STREET_NOT_FOUND", "Không xác định được tên đường tại vị trí này.");
    var compound = f.GetOrNull("compound");
    var loc = f.GetPath("geometry", "location");
    var address = f.GetStringOrNull("formatted_address") ?? f.GetStringOrNull("address") ?? "";
    return new StreetResult(f.GetStringOrNull("name") ?? address, address,
      loc?.GetDoubleOrNull("lat") ?? lat, loc?.GetDoubleOrNull("lng") ?? lng,
      compound?.GetStringOrNull("commune"), compound?.GetStringOrNull("province"),
      f.GetStringOrNull("place_id"), f.GetPath("geometry")?.GetStringOrNull("boundary"));
  }

  /// <summary>Một phần tử results[] của Geocode V2 (forward/reverse dùng chung cấu trúc).</summary>
  private static GeocodeResult Parse(JsonElement e)
  {
    var loc = e.GetPath("geometry", "location");
    var compound = e.GetOrNull("compound");
    var address = e.GetStringOrNull("formatted_address") ?? e.GetStringOrNull("address") ?? "";
    return new GeocodeResult(e.GetStringOrNull("place_id") ?? "", e.GetStringOrNull("name") ?? address, address,
      loc?.GetDoubleOrNull("lat") ?? 0, loc?.GetDoubleOrNull("lng") ?? 0,
      compound?.GetStringOrNull("commune"), compound?.GetStringOrNull("province"), e.GetStringArray("types"),
      e.GetStringOrNull("deprecated_description"));
  }
}
