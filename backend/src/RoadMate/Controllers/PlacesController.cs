namespace RoadMate.Controllers;

public class PlacesController(IPlaceService svc) : ApiControllerBase
{
  /// <remarks>⚠ CẦN CẤU HÌNH Goong:ApiKey. Toàn bộ nhóm places/* và routes/* dùng chung key này.</remarks>
  [HttpGet("places/autocomplete")]
  public async Task<IActionResult> Autocomplete([FromQuery] string q, [FromQuery] double? lat, [FromQuery] double? lng, CancellationToken ct) =>
    Handle(await svc.AutocompleteAsync(q, lat, lng, ct));

  [HttpGet("places/reverse")]
  public async Task<IActionResult> Reverse([FromQuery] double lat, [FromQuery] double lng, CancellationToken ct) => Handle(await svc.ReverseAsync(lat, lng, ct));

  [HttpGet("places/{placeId}")]
  public async Task<IActionResult> Detail(string placeId, CancellationToken ct) => Handle(await svc.DetailAsync(placeId, ct));
}
