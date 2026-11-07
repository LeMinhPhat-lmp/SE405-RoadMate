namespace RoadMate.Services.Maps;

public interface IGeocodingService
{
  /// <summary>Toạ độ → địa chỉ gần nhất (Geocode V2, latlng). Có cache theo toạ độ làm tròn ~11 m.</summary>
  Task<Result<ReverseResult>> ReverseAsync(double lat, double lng, CancellationToken ct);
  /// <summary>Địa chỉ chữ → toạ độ (Geocode V2, address).</summary>
  Task<Result<List<GeocodeResult>>> ForwardAsync(string address, int? limit, CancellationToken ct);
  /// <summary>Toạ độ → tên đường đang đi (Geocode Street V2).</summary>
  Task<Result<StreetResult>> StreetAsync(double lat, double lng, CancellationToken ct);
}
