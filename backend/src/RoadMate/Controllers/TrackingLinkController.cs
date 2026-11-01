namespace RoadMate.Controllers;

public class TrackingLinkController(ITrackingLinkService svc) : ApiControllerBase
{
  /// <remarks>⚠ Link trỏ tới Tracking:BaseUrl (giá trị mẫu, chưa có web nhận link). Xem docs/SETUP_AND_LIMITATIONS.md.</remarks>
  [HttpPost("trips/{id:guid}/tracking-link")]
  public async Task<IActionResult> Create(Guid id, CancellationToken ct) => Handle(await svc.CreateLinkAsync(UserId, id, ct));

  [HttpDelete("trips/{id:guid}/tracking-link")]
  public async Task<IActionResult> Disable(Guid id, CancellationToken ct) => Handle(await svc.DisableLinkAsync(UserId, id, ct));
}
