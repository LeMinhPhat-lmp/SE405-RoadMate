namespace RoadMate.Controllers;

public class TripsController : ApiControllerBase
{
    private readonly ITripService _trips; private readonly ITrackingService _tracking; private readonly IJournalService _journal;
    public TripsController(ITripService trips, ITrackingService tracking, IJournalService journal) { _trips = trips; _tracking = tracking; _journal = journal; }

    [HttpGet("trips")]
    public async Task<IActionResult> List([FromQuery] string? status, CancellationToken ct) => Handle(await _trips.ListAsync(Uid, status, ct));

    [HttpPost("trips")]
    public async Task<IActionResult> Create(TripUpsertRequest req, CancellationToken ct) => HandleCreated(await _trips.CreateAsync(Uid, req, ct));

    [HttpGet("trips/{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct) => Handle(await _trips.GetAsync(Uid, id, ct));

    [HttpPut("trips/{id:guid}")]
    public async Task<IActionResult> Update(Guid id, TripUpsertRequest req, CancellationToken ct) => Handle(await _trips.UpdateAsync(Uid, id, req, ct));

    [HttpDelete("trips/{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct) => Handle(await _trips.DeleteAsync(Uid, id, ct));

    [HttpPost("trips/{id:guid}/start")]
    public async Task<IActionResult> Start(Guid id, StartTripRequest? req, CancellationToken ct) =>
        Handle(await _trips.StartAsync(Uid, id, req ?? new StartTripRequest(null, null, null), ct));

    [HttpPost("trips/{id:guid}/finish")]
    public async Task<IActionResult> Finish(Guid id, CancellationToken ct) => Handle(await _trips.FinishAsync(Uid, id, ct));

    [HttpGet("trips/{id:guid}/summary")]
    public async Task<IActionResult> Summary(Guid id, CancellationToken ct) => Handle(await _trips.SummaryAsync(Uid, id, ct));

    [HttpPost("trips/{id:guid}/rest")]
    public async Task<IActionResult> SetRest(Guid id, RestRequest req, CancellationToken ct) => Handle(await _trips.SetRestAsync(Uid, id, req.Minutes, ct));

    [HttpDelete("trips/{id:guid}/rest")]
    public async Task<IActionResult> ClearRest(Guid id, CancellationToken ct) => Handle(await _trips.ClearRestAsync(Uid, id, ct));

    // ---- GPS / heartbeat
    [HttpPost("trips/{id:guid}/points")]
    public async Task<IActionResult> AddPoints(Guid id, AddPointsRequest req, CancellationToken ct) => Handle(await _tracking.AddPointsAsync(Uid, id, req, ct));

    [HttpPost("trips/{id:guid}/heartbeat")]
    public async Task<IActionResult> Heartbeat(Guid id, HeartbeatRequest req, CancellationToken ct) => Handle(await _tracking.HeartbeatAsync(Uid, id, req, ct));

    // ---- Tracking link (P1)
    [HttpPost("trips/{id:guid}/tracking-link")]
    public async Task<IActionResult> CreateLink(Guid id, CancellationToken ct) => Handle(await _tracking.CreateLinkAsync(Uid, id, ct));

    [HttpDelete("trips/{id:guid}/tracking-link")]
    public async Task<IActionResult> DisableLink(Guid id, CancellationToken ct) => Handle(await _tracking.DisableLinkAsync(Uid, id, ct));

    // ---- Journal
    [HttpGet("trips/{id:guid}/journal")]
    public async Task<IActionResult> Journal(Guid id, [FromQuery] int tzOffset = 7, CancellationToken ct = default) =>
        Handle(await _journal.GetJournalAsync(Uid, id, Math.Clamp(tzOffset, -12, 14), ct));

    [HttpGet("trips/{id:guid}/memories")]
    public async Task<IActionResult> Memories(Guid id, CancellationToken ct) => Handle(await _journal.ListMemoriesAsync(Uid, id, ct));

    [HttpPost("trips/{id:guid}/memories")]
    public async Task<IActionResult> AddMemory(Guid id, MemoryRequest req, CancellationToken ct) => HandleCreated(await _journal.AddMemoryAsync(Uid, id, req, ct));

    [HttpPut("memories/{memoryId:guid}")]
    public async Task<IActionResult> UpdateMemory(Guid memoryId, MemoryRequest req, CancellationToken ct) => Handle(await _journal.UpdateMemoryAsync(Uid, memoryId, req, ct));

    [HttpDelete("memories/{memoryId:guid}")]
    public async Task<IActionResult> DeleteMemory(Guid memoryId, CancellationToken ct) => Handle(await _journal.DeleteMemoryAsync(Uid, memoryId, ct));
}
