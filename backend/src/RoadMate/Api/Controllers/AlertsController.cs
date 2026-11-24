using Microsoft.AspNetCore.Mvc;
using RoadMate.Application.Alerts.Interfaces;
using RoadMate.Application.Alerts.DTOs.Requests;

namespace RoadMate.Api.Controllers;

public class AlertsController(IAlertService svc, IAlertLifecycleService lifecycle) : ApiControllerBase
{
  /// <remarks> 
  /// ⚠ CẦN CẤU HÌNH để chạy đúng mục đích: Sms:Provider=esms (+ ApiKey/SecretKey/Brandname) để gửi SMS thật; Firebase:CredentialPath cho push.
  /// Nếu chưa cấu hình, cảnh báo vẫn được tạo nhưng "gửi" chỉ là ghi log.
  /// </remarks>
  [HttpPost("trips/{tripId:guid}/alerts")]
  public async Task<IActionResult> Create(Guid tripId, CreateAlertRequest req, CancellationToken ct) => HandleCreated(await svc.CreateAsync(UserId, tripId, req, ct));

  [HttpGet("trips/{tripId:guid}/alerts")]
  public async Task<IActionResult> List(Guid tripId, CancellationToken ct) => Handle(await svc.ListAsync(UserId, tripId, ct));

  [HttpGet("trips/{tripId:guid}/alerts/paged")]
  public async Task<IActionResult> ListPaged(Guid tripId, [FromQuery] string? cursor, [FromQuery] int? limit, CancellationToken ct) =>
    Handle(await svc.ListPageAsync(UserId, tripId, cursor, limit, ct));

  [HttpPost("alerts/{id:guid}/locations")]
  public async Task<IActionResult> Location(Guid id, AlertLocationRequest req, CancellationToken ct) => Handle(await lifecycle.UpdateLocationAsync(UserId, id, req, ct));

  /// <summary>"Tôi ổn" - báo nhầm.</summary>
  [HttpPost("alerts/{id:guid}/cancel")]
  public async Task<IActionResult> Cancel(Guid id, CancellationToken ct) => Handle(await lifecycle.CancelAsync(UserId, id, ct));

  [HttpPost("alerts/{id:guid}/resolve")]
  public async Task<IActionResult> Resolve(Guid id, CancellationToken ct) => Handle(await lifecycle.ResolveAsync(UserId, id, ct));
}
