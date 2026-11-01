namespace RoadMate.Controllers;

public class JournalController(IJournalService svc) : ApiControllerBase
{
  [HttpGet("trips/{id:guid}/journal")]
  public async Task<IActionResult> Get(Guid id, [FromQuery] int tzOffset = 7, CancellationToken ct = default) =>
    Handle(await svc.GetJournalAsync(UserId, id, Math.Clamp(tzOffset, -12, 14), ct));
}
