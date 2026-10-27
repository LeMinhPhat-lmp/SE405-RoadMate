namespace RoadMate.Controllers;

public class MapsController : ApiControllerBase
{
    private readonly IMapService _svc;
    public MapsController(IMapService svc) => _svc = svc;

    [HttpPost("routes/directions")]
    public async Task<IActionResult> Directions(DirectionsRequest req, CancellationToken ct) => Handle(await _svc.DirectionsAsync(req, ct));

    [HttpGet("places/autocomplete")]
    public async Task<IActionResult> Autocomplete([FromQuery] string q, [FromQuery] double? lat, [FromQuery] double? lng, CancellationToken ct) =>
        Handle(await _svc.AutocompleteAsync(q, lat, lng, ct));

    [HttpGet("places/reverse")]
    public async Task<IActionResult> Reverse([FromQuery] double lat, [FromQuery] double lng, CancellationToken ct) => Handle(await _svc.ReverseAsync(lat, lng, ct));

    [HttpGet("places/{placeId}")]
    public async Task<IActionResult> Detail(string placeId, CancellationToken ct) => Handle(await _svc.PlaceDetailAsync(placeId, ct));
}
