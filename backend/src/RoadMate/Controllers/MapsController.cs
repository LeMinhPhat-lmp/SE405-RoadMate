namespace RoadMate.Controllers;

public class MapsController(IMapService svc) : ApiControllerBase
{
  /// <remarks>
  /// ⚠ CẦN CẤU HÌNH Goong:ApiKey (không có key sẽ trả lỗi MAP_NOT_CONFIGURED). avoidHighway/avoidToll hiện CHƯA được áp dụng vào tuyến.
  /// </remarks>
  [HttpPost("routes/directions")]
  public async Task<IActionResult> Directions(DirectionsRequest req, CancellationToken ct) => Handle(await svc.DirectionsAsync(req, ct));

  /// <remarks>⚠ CẦN CẤU HÌNH Goong:ApiKey. Toàn bộ nhóm places/* và routes/* dùng chung key này.</remarks>
  [HttpGet("places/autocomplete")]
  public async Task<IActionResult> Autocomplete([FromQuery] string q, [FromQuery] double? lat, [FromQuery] double? lng, CancellationToken ct) =>
    Handle(await svc.AutocompleteAsync(q, lat, lng, ct));

  [HttpGet("places/reverse")]
  public async Task<IActionResult> Reverse([FromQuery] double lat, [FromQuery] double lng, CancellationToken ct) => Handle(await svc.ReverseAsync(lat, lng, ct));

  [HttpGet("places/{placeId}")]
  public async Task<IActionResult> Detail(string placeId, CancellationToken ct) => Handle(await svc.PlaceDetailAsync(placeId, ct));
}
