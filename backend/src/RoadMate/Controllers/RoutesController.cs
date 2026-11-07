using Microsoft.AspNetCore.RateLimiting;

namespace RoadMate.Controllers;

[EnableRateLimiting("maps")]
public class RoutesController(IRoutingService svc) : ApiControllerBase
{
  /// <remarks>
  /// ⚠ CẦN CẤU HÌNH Goong:ApiKey (không có key trả 503 MAP_NOT_CONFIGURED). Gọi Goong Directions V2, mỗi chặng 1 request.
  /// Goong không có tham số tránh cao tốc/thu phí: khi bật avoidHighway/avoidToll server chọn tuyến thay thế nếu có,
  /// nếu không có sẽ trả cảnh báo trong <c>warnings</c> (ROUTE_HAS_HIGHWAY / ROUTE_HAS_TOLL).
  /// </remarks>
  [HttpPost("routes/directions")]
  public async Task<IActionResult> Directions(DirectionsRequest req, CancellationToken ct) => Handle(await svc.DirectionsAsync(req, ct));

  /// <remarks>⚠ CẦN CẤU HÌNH Goong:ApiKey. Distance Matrix V2: tối đa 10 điểm đi × 25 điểm đến. Dùng cho ETA nhanh, khoảng cách tới thành viên nhóm…</remarks>
  [HttpPost("routes/matrix")]
  public async Task<IActionResult> Matrix(DistanceMatrixRequest req, CancellationToken ct) => Handle(await svc.MatrixAsync(req, ct));
}
