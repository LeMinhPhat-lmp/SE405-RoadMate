namespace RoadMate.Controllers;

public class RoutesController(IRoutingService svc) : ApiControllerBase
{
  /// <remarks>
  /// ⚠ CẦN CẤU HÌNH Goong:ApiKey (không có key sẽ trả lỗi MAP_NOT_CONFIGURED). avoidHighway/avoidToll hiện CHƯA được áp dụng vào tuyến.
  /// </remarks>
  [HttpPost("routes/directions")]
  public async Task<IActionResult> Directions(DirectionsRequest req, CancellationToken ct) => Handle(await svc.DirectionsAsync(req, ct));
}
