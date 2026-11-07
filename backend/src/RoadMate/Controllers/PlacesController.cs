using Microsoft.AspNetCore.RateLimiting;

namespace RoadMate.Controllers;

/// <remarks>⚠ CẦN CẤU HÌNH Goong:ApiKey. Toàn bộ nhóm places/* và routes/* dùng chung key này.</remarks>
[EnableRateLimiting("maps")]
public class PlacesController(IPlaceService places, IGeocodingService geocoding) : ApiControllerBase
{
  /// <param name="withDistance">true: kèm distanceMeters từ (lat,lng) tới từng gợi ý.</param>
  [HttpGet("places/autocomplete")]
  public async Task<IActionResult> Autocomplete([FromQuery] string q, [FromQuery] double? lat, [FromQuery] double? lng,
    [FromQuery] int? limit, [FromQuery] bool withDistance, CancellationToken ct) =>
    Handle(await places.AutocompleteAsync(q, lat, lng, limit, withDistance, ct));

  /// <remarks>
  /// ⚠ XẤP XỈ: Goong V2 (theo bộ doc hiện có) không có API tìm theo loại tiện ích, nên server dùng Autocomplete theo từ khoá
  /// rồi lọc theo bán kính thật bằng toạ độ của Place Detail (tốn tới 10 request chi tiết, đã có cache). type: gas_station|repair|hospital|clinic|pharmacy|atm|lodging|homestay|food|cafe.
  /// </remarks>
  [HttpGet("places/nearby")]
  public async Task<IActionResult> Nearby([FromQuery] double lat, [FromQuery] double lng, [FromQuery] string type,
    [FromQuery] int radius, [FromQuery] int? limit, CancellationToken ct) =>
    Handle(await places.NearbyAsync(lat, lng, type, radius == 0 ? 5000 : radius, limit, ct));

  [HttpGet("places/reverse")]
  public async Task<IActionResult> Reverse([FromQuery] double lat, [FromQuery] double lng, CancellationToken ct) =>
    Handle(await geocoding.ReverseAsync(lat, lng, ct));

  /// <summary>Địa chỉ chữ → toạ độ.</summary>
  [HttpGet("places/geocode")]
  public async Task<IActionResult> Geocode([FromQuery] string address, [FromQuery] int? limit, CancellationToken ct) =>
    Handle(await geocoding.ForwardAsync(address, limit, ct));

  /// <summary>Tên đường tại một toạ độ (hiển thị "Đang đi trên …" khi dẫn đường).</summary>
  [HttpGet("places/street")]
  public async Task<IActionResult> Street([FromQuery] double lat, [FromQuery] double lng, CancellationToken ct) =>
    Handle(await geocoding.StreetAsync(lat, lng, ct));

  [HttpGet("places/{placeId}")]
  public async Task<IActionResult> Detail(string placeId, CancellationToken ct) => Handle(await places.DetailAsync(placeId, ct));

  /// <summary>Địa điểm con (cổng, sảnh, khu…) của địa điểm có hasChildren = true.</summary>
  [HttpGet("places/{placeId}/children")]
  public async Task<IActionResult> Children(string placeId, CancellationToken ct) => Handle(await places.ChildrenAsync(placeId, ct));
}
