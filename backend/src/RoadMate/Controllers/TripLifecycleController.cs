namespace RoadMate.Controllers;

public class TripLifecycleController(ITripLifecycleService svc) : ApiControllerBase
{
  [HttpPost("trips/{id:guid}/start")]
  public async Task<IActionResult> Start(Guid id, StartTripRequest? req, CancellationToken ct) =>
    Handle(await svc.StartAsync(UserId, id, req ?? new StartTripRequest(null, null, null), ct));

  [HttpPost("trips/{id:guid}/finish")]
  public async Task<IActionResult> Finish(Guid id, CancellationToken ct) => Handle(await svc.FinishAsync(UserId, id, ct));

  [HttpGet("trips/{id:guid}/summary")]
  public async Task<IActionResult> Summary(Guid id, CancellationToken ct) => Handle(await svc.SummaryAsync(UserId, id, ct));

  /// <remarks>⚠ Server chỉ LƯU RestUntil. Việc tạm ngừng cảnh báo dừng lâu do app (client) thực hiện.</remarks>
  [HttpPost("trips/{id:guid}/rest")]
  public async Task<IActionResult> SetRest(Guid id, RestRequest req, CancellationToken ct) => Handle(await svc.SetRestAsync(UserId, id, req.Minutes, ct));

  /// <summary>"Tôi vẫn ổn, đang đi tiếp": dời giờ dự kiến về (ngăn cảnh báo về trễ).</summary>
  [HttpPut("trips/{id:guid}/expected-return")]
  public async Task<IActionResult> ExtendReturn(Guid id, ExtendReturnRequest req, CancellationToken ct) =>
    Handle(await svc.ExtendReturnAsync(UserId, id, req.ExpectedReturnTime, ct));

  [HttpDelete("trips/{id:guid}/rest")]
  public async Task<IActionResult> ClearRest(Guid id, CancellationToken ct) => Handle(await svc.ClearRestAsync(UserId, id, ct));
}
