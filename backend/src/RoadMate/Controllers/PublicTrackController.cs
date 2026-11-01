using Microsoft.AspNetCore.RateLimiting;

namespace RoadMate.Controllers;

[AllowAnonymous]
[EnableRateLimiting("public")]
public class PublicTrackController(IPublicTrackingService svc) : ApiControllerBase
{
  /// <remarks>⚠ Chỉ là API: chưa có trang web người thân và domain (Tracking:BaseUrl) mới là giá trị mẫu.</remarks>
  [HttpGet("public/track/{token}")]
  public async Task<IActionResult> Get(string token, CancellationToken ct) => Handle(await svc.GetAsync(token, ct));

  [HttpGet("public/track/{token}/route")]
  public async Task<IActionResult> Route(string token, CancellationToken ct) => Handle(await svc.GetRouteAsync(token, ct));

  /// <remarks>
  /// ⚠ CHƯA NÊN DÙNG ở production: bất kỳ ai có link đều xác nhận được và DỪNG cảnh báo đang lặp. Cần cơ chế xác thực người thân trước.
  /// </remarks>
  [HttpPost("public/track/{token}/ack")]
  public async Task<IActionResult> Ack(string token, CancellationToken ct) => Handle(await svc.AckAsync(token, ct));
}
