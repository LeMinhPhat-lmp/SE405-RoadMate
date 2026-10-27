namespace RoadMate.Controllers;

public class AlertsController(IAlertService svc) : ApiControllerBase
{
  /// <remarks>
  /// ⚠ CẦN CẤU HÌNH để chạy đúng mục đích: Sms:Provider=esms (+ ApiKey/SecretKey/Brandname) để gửi SMS thật; Firebase:CredentialPath cho push.
  /// Nếu chưa cấu hình, cảnh báo vẫn được tạo nhưng "gửi" chỉ là ghi log.
  /// </remarks>
  [HttpPost("trips/{tripId:guid}/alerts")]
  public async Task<IActionResult> Create(Guid tripId, CreateAlertRequest req, CancellationToken ct) => HandleCreated(await svc.CreateAsync(UserId, tripId, req, ct));

  [HttpGet("trips/{tripId:guid}/alerts")]
  public async Task<IActionResult> List(Guid tripId, CancellationToken ct) => Handle(await svc.ListAsync(UserId, tripId, ct));

  [HttpPost("alerts/{id:guid}/locations")]
  public async Task<IActionResult> Location(Guid id, AlertLocationRequest req, CancellationToken ct) => Handle(await svc.UpdateLocationAsync(UserId, id, req, ct));

  /// <summary>"Tôi ổn" - báo nhầm.</summary>
  [HttpPost("alerts/{id:guid}/cancel")]
  public async Task<IActionResult> Cancel(Guid id, CancellationToken ct) => Handle(await svc.CancelAsync(UserId, id, ct));

  [HttpPost("alerts/{id:guid}/resolve")]
  public async Task<IActionResult> Resolve(Guid id, CancellationToken ct) => Handle(await svc.ResolveAsync(UserId, id, ct));
}

/// <summary>Chỉ bật khi Debug:EnableSimulator = true (dev).</summary>
public class DebugController(IAlertService svc, IConfiguration cfg) : ApiControllerBase
{
  /// <remarks>⚠ CHỈ DÙNG DEV/DEMO: cần Debug:EnableSimulator=true, tắt ở production. Gửi SMS/push thật nếu đã cấu hình.</remarks>
  [HttpPost("debug/simulate-alert")]
  public async Task<IActionResult> Simulate(SimulateAlertRequest req, CancellationToken ct)
  {
    if (!cfg.GetValue<bool>("Debug:EnableSimulator")) return NotFound();
    var type = string.IsNullOrWhiteSpace(req.Type) ? AlertType.Crash : req.Type;
    var r = await svc.CreateAsync(UserId, req.TripId,
      new CreateAlertRequest("sim-" + Guid.NewGuid().ToString("N"), type, req.Lat ?? 10.7769, req.Lng ?? 106.7009, 80), ct);
    return HandleCreated(r);
  }
}

[AllowAnonymous]
[Microsoft.AspNetCore.RateLimiting.EnableRateLimiting("public")]
public class PublicTrackController(ITrackingService svc) : ApiControllerBase
{
  /// <remarks>⚠ Chỉ là API: chưa có trang web người thân và domain (Tracking:BaseUrl) mới là giá trị mẫu.</remarks>
  [HttpGet("public/track/{token}")]
  public async Task<IActionResult> Get(string token, CancellationToken ct) => Handle(await svc.GetPublicAsync(token, ct));

  [HttpGet("public/track/{token}/route")]
  public async Task<IActionResult> Route(string token, CancellationToken ct) => Handle(await svc.GetPublicRouteAsync(token, ct));

  /// <remarks>
  /// ⚠ CHƯA NÊN DÙNG ở production: bất kỳ ai có link đều xác nhận được và DỪNG cảnh báo đang lặp. Cần cơ chế xác thực người thân trước.
  /// </remarks>
  [HttpPost("public/track/{token}/ack")]
  public async Task<IActionResult> Ack(string token, CancellationToken ct) => Handle(await svc.PublicAckAsync(token, ct));
}
