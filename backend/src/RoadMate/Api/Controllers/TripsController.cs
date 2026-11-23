using Microsoft.AspNetCore.Mvc;
using RoadMate.Application.Trips.Interfaces;
using RoadMate.Application.Trips.DTOs.Requests;

namespace RoadMate.Api.Controllers;

public class TripsController(ITripService svc) : ApiControllerBase
{
  [HttpGet("trips")]
  public async Task<IActionResult> List([FromQuery] string? status, CancellationToken ct) => Handle(await svc.ListAsync(UserId, status, ct));

  [HttpGet("trips/paged")]
  public async Task<IActionResult> ListPaged([FromQuery] string? status, [FromQuery] string? cursor, [FromQuery] int? limit, CancellationToken ct) =>
    Handle(await svc.ListPageAsync(UserId, status, cursor, limit, ct));

  [HttpPost("trips")]
  public async Task<IActionResult> Create(TripUpsertRequest req, CancellationToken ct) => HandleCreated(await svc.CreateAsync(UserId, req, ct));

  [HttpGet("trips/{id:guid}")]
  public async Task<IActionResult> Get(Guid id, CancellationToken ct) => Handle(await svc.GetAsync(UserId, id, ct));

  [HttpPut("trips/{id:guid}")]
  public async Task<IActionResult> Update(Guid id, TripUpsertRequest req, CancellationToken ct) => Handle(await svc.UpdateAsync(UserId, id, req, ct));

  [HttpDelete("trips/{id:guid}")]
  public async Task<IActionResult> Delete(Guid id, CancellationToken ct) => Handle(await svc.DeleteAsync(UserId, id, ct));
}
