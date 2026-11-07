using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using RoadMate.Infrastructure.Maps;

namespace RoadMate.Services.Maps;

public class PlaceService(IGoongClient goong, IMemoryCache cache, IOptions<GoongOptions> options) : IPlaceService
{
  public const int MaxQueryLength = 200;
  public const int MaxLimit = 10;
  public const int MaxPlaceIdLength = 1000;
  public const int MaxNearbyRadiusM = 50_000;

  public async Task<Result<List<PlacePrediction>>> AutocompleteAsync(string q, double? lat, double? lng, int? limit, bool withDistance, CancellationToken ct)
  {
    q = (q ?? "").Trim();
    if (q.Length == 0) return Error.Validation("EMPTY_QUERY", "Thiếu từ khoá tìm kiếm.");
    if (q.Length > MaxQueryLength) return Error.Validation("QUERY_TOO_LONG", $"Từ khoá tối đa {MaxQueryLength} ký tự.");
    if ((lat is null) != (lng is null)) return Error.Validation("INVALID_LOCATION", "lat và lng phải đi cùng nhau.");
    if (lat is not null && !Geo.ValidLatLng(lat.Value, lng!.Value)) return Errors.InvalidLocation;
    var n = Math.Clamp(limit ?? 5, 1, MaxLimit);

    var o = options.Value;
    var key = $"ac:{q.ToLowerInvariant()}:{lat?.ToString("F3")}:{lng?.ToString("F3")}:{n}:{withDistance}";
    return await cache.GetOrAddAsync<List<PlacePrediction>>(key, TimeSpan.FromSeconds(o.AutocompleteCacheSeconds), async () =>
    {
      var query = new GoongQuery().Add("input", q).Add("limit", n)
        .AddIf(o.IncludeDeprecatedAdministrativeUnit, "has_deprecated_administrative_unit", "true");
      if (lat is not null) { query.AddLatLng("location", lat.Value, lng!.Value); if (withDistance) query.AddLatLng("origin", lat.Value, lng.Value); }
      var r = await goong.GetAsync("v2/place/autocomplete", query, ct);
      return r.IsFailure ? r.Error : Result.Success(ParsePredictions(r.Value));
    });
  }

  public async Task<Result<PlaceDetail>> DetailAsync(string placeId, CancellationToken ct)
  {
    var invalid = ValidatePlaceId(placeId);
    if (invalid is not null) return invalid;
    var o = options.Value;
    return await cache.GetOrAddAsync<PlaceDetail>($"pd:{placeId}", TimeSpan.FromMinutes(o.DetailCacheMinutes), async () =>
    {
      var query = new GoongQuery().Add("place_id", placeId).AddIf(o.IncludeDeprecatedAdministrativeUnit, "has_deprecated_administrative_unit", "true");
      var r = await goong.GetAsync("v2/place/detail", query, ct);
      if (r.IsFailure) return r.Error;
      if (r.Value.GetOrNull("result") is not { ValueKind: JsonValueKind.Object } res ||
          res.GetPath("geometry", "location") is not { } loc ||
          loc.GetDoubleOrNull("lat") is not { } lat || loc.GetDoubleOrNull("lng") is not { } lng)
        return Error.NotFound("PLACE_NOT_FOUND", "Không tìm thấy địa điểm.");
      var address = res.GetStringOrNull("formatted_address");
      var compound = res.GetOrNull("compound");
      return Result.Success(new PlaceDetail(placeId, res.GetStringOrNull("name") ?? address ?? "", address, lat, lng,
        compound?.GetStringOrNull("commune"), compound?.GetStringOrNull("province"), res.GetStringArray("types"),
        res.GetStringOrNull("url"), res.GetStringOrNull("deprecated_description")));
    });
  }

  public async Task<Result<List<PlacePrediction>>> ChildrenAsync(string parentPlaceId, CancellationToken ct)
  {
    var invalid = ValidatePlaceId(parentPlaceId);
    if (invalid is not null) return invalid;
    var o = options.Value;
    return await cache.GetOrAddAsync<List<PlacePrediction>>($"pc:{parentPlaceId}", TimeSpan.FromMinutes(o.DetailCacheMinutes), async () =>
    {
      var query = new GoongQuery().Add("parent_id", parentPlaceId).AddIf(o.IncludeDeprecatedAdministrativeUnit, "has_deprecated_administrative_unit", "true");
      var r = await goong.GetAsync("v2/place/children", query, ct);
      return r.IsFailure ? r.Error : Result.Success(ParsePredictions(r.Value));
    });
  }

  public async Task<Result<List<NearbyPlace>>> NearbyAsync(double lat, double lng, string type, int radiusM, int? limit, CancellationToken ct)
  {
    if (!Geo.ValidLatLng(lat, lng)) return Errors.InvalidLocation;
    if (!NearbyCatalog.TryGetKeyword(type, out var keyword))
      return Error.Validation("INVALID_TYPE", $"type phải là một trong: {string.Join(", ", NearbyCatalog.Types)}.");
    if (radiusM is < 100 or > MaxNearbyRadiusM) return Error.Validation("INVALID_RADIUS", $"radius (mét) phải từ 100 đến {MaxNearbyRadiusM}.");
    var n = Math.Clamp(limit ?? 5, 1, MaxLimit);

    var o = options.Value;
    var key = $"nb:{type.ToLowerInvariant()}:{lat:F3}:{lng:F3}:{radiusM}:{n}";
    return await cache.GetOrAddAsync<List<NearbyPlace>>(key, TimeSpan.FromMinutes(o.NearbyCacheMinutes), async () =>
    {
      // Autocomplete không trả toạ độ → lấy tối đa 10 gợi ý rồi gọi Place Detail (có cache) để lọc theo bán kính thật.
      var ac = await AutocompleteAsync(keyword, lat, lng, MaxLimit, false, ct);
      if (ac.IsFailure) return ac.Error;

      using var gate = new SemaphoreSlim(Math.Clamp(o.MaxParallelRequests, 1, 8));
      var details = await Task.WhenAll(ac.Value.Where(p => !string.IsNullOrEmpty(p.PlaceId)).Select(async p =>
      {
        await gate.WaitAsync(ct);
        try { return await DetailAsync(p.PlaceId, ct); }
        finally { gate.Release(); }
      }));

      // Nếu MỌI lần lấy chi tiết đều lỗi (key sai, hết hạn mức…) thì báo lỗi thay vì trả danh sách rỗng gây hiểu nhầm.
      if (details.Length > 0 && details.All(d => d.IsFailure)) return details[0].Error;

      var found = details.Where(d => d.IsSuccess).Select(d => d.Value)
        .Select(d => (Detail: d, Dist: (int)Math.Round(Geo.HaversineMeters(lat, lng, d.Lat, d.Lng))))
        .Where(x => x.Dist <= radiusM)
        .OrderBy(x => x.Dist).Take(n)
        .Select(x => new NearbyPlace(x.Detail.PlaceId, x.Detail.Name, x.Detail.Address, x.Detail.Lat, x.Detail.Lng, x.Dist, x.Detail.Types))
        .ToList();
      return Result.Success(found);
    });
  }

  private static List<PlacePrediction> ParsePredictions(JsonElement root) =>
    root.GetArray("predictions").Select(p =>
    {
      var sf = p.GetOrNull("structured_formatting");
      var compound = p.GetOrNull("compound");
      return new PlacePrediction(p.GetStringOrNull("place_id") ?? "", p.GetStringOrNull("description") ?? "",
        sf?.GetStringOrNull("main_text"), sf?.GetStringOrNull("secondary_text"), p.GetBoolOrDefault("has_children"),
        p.GetStringArray("types"), compound?.GetStringOrNull("commune"), compound?.GetStringOrNull("province"),
        p.GetDoubleOrNull("distance_meters"), p.GetStringOrNull("deprecated_description"));
    }).Where(p => p.PlaceId.Length > 0).ToList();

  private static Error? ValidatePlaceId(string? id) =>
    string.IsNullOrWhiteSpace(id) || id.Length > MaxPlaceIdLength
      ? Error.Validation("INVALID_PLACE_ID", "placeId không hợp lệ.") : null;
}
