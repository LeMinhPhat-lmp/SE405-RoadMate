namespace RoadMate.Controllers;

/// <remarks>⚠ CẦN CẤU HÌNH Firebase:CredentialPath (service account hợp lệ) + Storage:Bucket; chưa có thì trả 503 STORAGE_NOT_CONFIGURED.</remarks>
public class MediaController(IMediaService svc) : ApiControllerBase
{
  /// <summary>
  /// Xin URL đã ký để upload ảnh/video thẳng lên Firebase Storage. Client PUT file tới uploadUrl kèm ĐÚNG các header trong headers,
  /// sau đó gọi POST trips/{id}/memories với mediaPath.
  /// </summary>
  [HttpPost("trips/{tripId:guid}/media/upload-url")]
  public async Task<IActionResult> UploadUrl(Guid tripId, UploadUrlRequest req, CancellationToken ct) =>
    Handle(await svc.CreateUploadUrlAsync(UserId, tripId, req, ct));

  /// <summary>Cấp lại URL đọc có hạn cho media của kỷ niệm (khi mediaUrl cũ đã hết hạn).</summary>
  [HttpGet("memories/{memoryId:guid}/media-url")]
  public async Task<IActionResult> DownloadUrl(Guid memoryId, CancellationToken ct) =>
    Handle(await svc.GetDownloadUrlAsync(UserId, memoryId, ct));
}
