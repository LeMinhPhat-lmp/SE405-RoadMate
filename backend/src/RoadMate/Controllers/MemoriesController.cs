namespace RoadMate.Controllers;

public class MemoriesController(IMemoryService svc) : ApiControllerBase
{
  [HttpGet("trips/{id:guid}/memories")]
  public async Task<IActionResult> List(Guid id, CancellationToken ct) => Handle(await svc.ListAsync(UserId, id, ct));

  /// <remarks>⚠ mediaUrl do client upload lên Firebase Storage; server KHÔNG kiểm tra URL có hợp lệ/thuộc về user hay không.</remarks>
  [HttpPost("trips/{id:guid}/memories")]
  public async Task<IActionResult> Add(Guid id, MemoryRequest req, CancellationToken ct) => HandleCreated(await svc.AddAsync(UserId, id, req, ct));

  [HttpPut("memories/{memoryId:guid}")]
  public async Task<IActionResult> Update(Guid memoryId, MemoryRequest req, CancellationToken ct) => Handle(await svc.UpdateAsync(UserId, memoryId, req, ct));

  [HttpDelete("memories/{memoryId:guid}")]
  public async Task<IActionResult> Delete(Guid memoryId, CancellationToken ct) => Handle(await svc.DeleteAsync(UserId, memoryId, ct));
}
