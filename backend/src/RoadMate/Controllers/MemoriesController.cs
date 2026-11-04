namespace RoadMate.Controllers;

public class MemoriesController(IMemoryService svc) : ApiControllerBase
{
  [HttpGet("trips/{id:guid}/memories")]
  public async Task<IActionResult> List(Guid id, CancellationToken ct) => Handle(await svc.ListAsync(UserId, id, ct));

  /// <remarks>Ảnh/video: xin URL upload ở POST trips/{id}/media/upload-url, upload xong gửi mediaPath ở đây. Server kiểm tra mediaPath thuộc đúng chuyến và (mặc định) file đã tồn tại.</remarks>
  [HttpPost("trips/{id:guid}/memories")]
  public async Task<IActionResult> Add(Guid id, MemoryRequest req, CancellationToken ct) => HandleCreated(await svc.AddAsync(UserId, id, req, ct));

  [HttpPut("memories/{memoryId:guid}")]
  public async Task<IActionResult> Update(Guid memoryId, MemoryRequest req, CancellationToken ct) => Handle(await svc.UpdateAsync(UserId, memoryId, req, ct));

  [HttpDelete("memories/{memoryId:guid}")]
  public async Task<IActionResult> Delete(Guid memoryId, CancellationToken ct) => Handle(await svc.DeleteAsync(UserId, memoryId, ct));
}
