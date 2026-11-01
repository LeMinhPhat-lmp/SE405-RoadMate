namespace RoadMate.Services.Maps;

public interface IPlaceService
{
  /// <param name="withDistance">true: gửi origin=location để Goong trả thêm distance_meters.</param>
  Task<Result<List<PlacePrediction>>> AutocompleteAsync(string q, double? lat, double? lng, int? limit, bool withDistance, CancellationToken ct);
  Task<Result<PlaceDetail>> DetailAsync(string placeId, CancellationToken ct);
  /// <summary>Các địa điểm con (cổng, sảnh, khu…) của một địa điểm lớn (has_children = true).</summary>
  Task<Result<List<PlacePrediction>>> ChildrenAsync(string parentPlaceId, CancellationToken ct);
  /// <summary>Tìm tiện ích quanh vị trí (trạm xăng, sửa xe, bệnh viện…). Xấp xỉ bằng Autocomplete + Place Detail, xem docs/GOONG_INTEGRATION.md.</summary>
  Task<Result<List<NearbyPlace>>> NearbyAsync(double lat, double lng, string type, int radiusM, int? limit, CancellationToken ct);
}
