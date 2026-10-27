namespace RoadMate.Controllers;

public class AlertsController : ApiControllerBase
{
    private readonly IAlertService _svc;
    public AlertsController(IAlertService svc) => _svc = svc;

    [HttpPost("trips/{tripId:guid}/alerts")]
    public async Task<IActionResult> Create(Guid tripId, CreateAlertRequest req, CancellationToken ct) => HandleCreated(await _svc.CreateAsync(Uid, tripId, req, ct));

    [HttpGet("trips/{tripId:guid}/alerts")]
    public async Task<IActionResult> List(Guid tripId, CancellationToken ct) => Handle(await _svc.ListAsync(Uid, tripId, ct));

    [HttpPost("alerts/{id:guid}/locations")]
    public async Task<IActionResult> Location(Guid id, AlertLocationRequest req, CancellationToken ct) => Handle(await _svc.UpdateLocationAsync(Uid, id, req, ct));

    /// <summary>"Tôi ổn" - báo nhầm.</summary>
    [HttpPost("alerts/{id:guid}/cancel")]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken ct) => Handle(await _svc.CancelAsync(Uid, id, ct));

    [HttpPost("alerts/{id:guid}/resolve")]
    public async Task<IActionResult> Resolve(Guid id, CancellationToken ct) => Handle(await _svc.ResolveAsync(Uid, id, ct));
}

/// <summary>Chỉ bật khi Debug:EnableSimulator = true (dev).</summary>
public class DebugController : ApiControllerBase
{
    private readonly IAlertService _svc; private readonly IConfiguration _cfg;
    public DebugController(IAlertService svc, IConfiguration cfg) { _svc = svc; _cfg = cfg; }

    [HttpPost("debug/simulate-alert")]
    public async Task<IActionResult> Simulate(SimulateAlertRequest req, CancellationToken ct)
    {
        if (!_cfg.GetValue<bool>("Debug:EnableSimulator")) return NotFound();
        var type = string.IsNullOrWhiteSpace(req.Type) ? AlertType.Crash : req.Type;
        var r = await _svc.CreateAsync(Uid, req.TripId,
            new CreateAlertRequest("sim-" + Guid.NewGuid().ToString("N"), type, req.Lat ?? 10.7769, req.Lng ?? 106.7009, 80), ct);
        return HandleCreated(r);
    }
}

[AllowAnonymous]
[Microsoft.AspNetCore.RateLimiting.EnableRateLimiting("public")]
public class PublicTrackController : ApiControllerBase
{
    private readonly ITrackingService _svc;
    public PublicTrackController(ITrackingService svc) => _svc = svc;

    [HttpGet("public/track/{token}")]
    public async Task<IActionResult> Get(string token, CancellationToken ct) => Handle(await _svc.GetPublicAsync(token, ct));

    [HttpGet("public/track/{token}/route")]
    public async Task<IActionResult> Route(string token, CancellationToken ct) => Handle(await _svc.GetPublicRouteAsync(token, ct));

    [HttpPost("public/track/{token}/ack")]
    public async Task<IActionResult> Ack(string token, CancellationToken ct) => Handle(await _svc.PublicAckAsync(token, ct));
}
