namespace RoadMate.Controllers;

public class TripsController(ITripService trips, ITrackingService tracking, IJournalService journal) : ApiControllerBase
{
  [HttpGet("trips")]
  public async Task<IActionResult> List([FromQuery] string? status, CancellationToken ct) => Handle(await trips.ListAsync(UserId, status, ct));

  [HttpPost("trips")]
  public async Task<IActionResult> Create(TripUpsertRequest req, CancellationToken ct) => HandleCreated(await trips.CreateAsync(UserId, req, ct));

  [HttpGet("trips/{id:guid}")]
  public async Task<IActionResult> Get(Guid id, CancellationToken ct) => Handle(await trips.GetAsync(UserId, id, ct));

  [HttpPut("trips/{id:guid}")]
  public async Task<IActionResult> Update(Guid id, TripUpsertRequest req, CancellationToken ct) => Handle(await trips.UpdateAsync(UserId, id, req, ct));

  [HttpDelete("trips/{id:guid}")]
  public async Task<IActionResult> Delete(Guid id, CancellationToken ct) => Handle(await trips.DeleteAsync(UserId, id, ct));

  [HttpPost("trips/{id:guid}/start")]
  public async Task<IActionResult> Start(Guid id, StartTripRequest? req, CancellationToken ct) =>
    Handle(await trips.StartAsync(UserId, id, req ?? new StartTripRequest(null, null, null), ct));

  [HttpPost("trips/{id:guid}/finish")]
  public async Task<IActionResult> Finish(Guid id, CancellationToken ct) => Handle(await trips.FinishAsync(UserId, id, ct));

  [HttpGet("trips/{id:guid}/summary")]
  public async Task<IActionResult> Summary(Guid id, CancellationToken ct) => Handle(await trips.SummaryAsync(UserId, id, ct));

  /// <remarks>⚠ Server chỉ LƯU RestUntil. Việc tạm ngừng cảnh báo dừng lâu do app (client) thực hiện.</remarks>
  [HttpPost("trips/{id:guid}/rest")]
  public async Task<IActionResult> SetRest(Guid id, RestRequest req, CancellationToken ct) => Handle(await trips.SetRestAsync(UserId, id, req.Minutes, ct));

  [HttpDelete("trips/{id:guid}/rest")]
  public async Task<IActionResult> ClearRest(Guid id, CancellationToken ct) => Handle(await trips.ClearRestAsync(UserId, id, ct));

  // ---- GPS / heartbeat
  [HttpPost("trips/{id:guid}/points")]
  public async Task<IActionResult> AddPoints(Guid id, AddPointsRequest req, CancellationToken ct) => Handle(await tracking.AddPointsAsync(UserId, id, req, ct));

  /// <remarks>
  /// ⚠ CHỈ LƯU vị trí/pin/mạng. Chưa có job nền dùng dữ liệu này (mất sóng, pin yếu, về trễ, tụt đoàn) nên các cảnh báo đó
  /// hiện chỉ phát sinh khi app tự gọi POST alerts.
  /// </remarks>
  [HttpPost("trips/{id:guid}/heartbeat")]
  public async Task<IActionResult> Heartbeat(Guid id, HeartbeatRequest req, CancellationToken ct) => Handle(await tracking.HeartbeatAsync(UserId, id, req, ct));

  // ---- Tracking link (P1)
  /// <remarks>⚠ Link trỏ tới Tracking:BaseUrl (giá trị mẫu, chưa có web nhận link). Xem docs/SETUP_AND_LIMITATIONS.md.</remarks>
  [HttpPost("trips/{id:guid}/tracking-link")]
  public async Task<IActionResult> CreateLink(Guid id, CancellationToken ct) => Handle(await tracking.CreateLinkAsync(UserId, id, ct));

  [HttpDelete("trips/{id:guid}/tracking-link")]
  public async Task<IActionResult> DisableLink(Guid id, CancellationToken ct) => Handle(await tracking.DisableLinkAsync(UserId, id, ct));

  // ---- Journal
  [HttpGet("trips/{id:guid}/journal")]
  public async Task<IActionResult> Journal(Guid id, [FromQuery] int tzOffset = 7, CancellationToken ct = default) =>
    Handle(await journal.GetJournalAsync(UserId, id, Math.Clamp(tzOffset, -12, 14), ct));

  [HttpGet("trips/{id:guid}/memories")]
  public async Task<IActionResult> Memories(Guid id, CancellationToken ct) => Handle(await journal.ListMemoriesAsync(UserId, id, ct));

  /// <remarks>⚠ mediaUrl do client upload lên Firebase Storage; server KHÔNG kiểm tra URL có hợp lệ/thuộc về user hay không.</remarks>
  [HttpPost("trips/{id:guid}/memories")]
  public async Task<IActionResult> AddMemory(Guid id, MemoryRequest req, CancellationToken ct) => HandleCreated(await journal.AddMemoryAsync(UserId, id, req, ct));

  [HttpPut("memories/{memoryId:guid}")]
  public async Task<IActionResult> UpdateMemory(Guid memoryId, MemoryRequest req, CancellationToken ct) => Handle(await journal.UpdateMemoryAsync(UserId, memoryId, req, ct));

  [HttpDelete("memories/{memoryId:guid}")]
  public async Task<IActionResult> DeleteMemory(Guid memoryId, CancellationToken ct) => Handle(await journal.DeleteMemoryAsync(UserId, memoryId, ct));
}
