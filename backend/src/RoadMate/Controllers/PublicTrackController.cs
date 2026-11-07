using Microsoft.AspNetCore.RateLimiting;

namespace RoadMate.Controllers;

[AllowAnonymous]
[EnableRateLimiting("public")]
public class PublicTrackController(IPublicTrackingService tracking, IAlertAckService acks) : ApiControllerBase
{
  /// <remarks>⚠ Chỉ là API: chưa có trang web người thân và domain (Tracking:BaseUrl) mới là giá trị mẫu.</remarks>
  [HttpGet("public/track/{token}")]
  public async Task<IActionResult> Get(string token, CancellationToken ct) => Handle(await tracking.GetAsync(token, ct));

  [HttpGet("public/track/{token}/route")]
  public async Task<IActionResult> Route(string token, CancellationToken ct) => Handle(await tracking.GetRouteAsync(token, ct));

  /// <summary>
  /// Người thân bấm link xác nhận trong SMS ("đã liên lạc được"). Token riêng của từng liên hệ: chỉ dừng SMS lặp cho người đó,
  /// cảnh báo vẫn mở cho tới khi chủ chuyến bấm "Tôi ổn". Thay thế endpoint cũ public/track/{token}/ack (đã bỏ vì không an toàn).
  /// </summary>
  [HttpPost("public/ack/{token}")]
  public async Task<IActionResult> Ack(string token, CancellationToken ct) => Handle(await acks.AckAsync(token, ct));
}
