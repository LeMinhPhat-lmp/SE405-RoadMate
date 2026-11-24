using RoadMate.Application.Abstractions.Persistence;
using RoadMate.Application.Abstractions.Storage;
using RoadMate.Application.Common;
using RoadMate.Application.Journal.DTOs.Requests;
using RoadMate.Application.Journal.DTOs.Responses;
using RoadMate.Application.Journal.Interfaces;
using RoadMate.Application.Journal.Mappers;
using RoadMate.Application.Shared;
using RoadMate.Domain.Common;
using RoadMate.Domain.Trips;

namespace RoadMate.Application.Journal.Implements;

public class MemoryService(ITripRepository trips, IMemoryRepository memories, IMediaReferenceChecker media, IUnitOfWork uow, IMediaStorage storage,
  TimeProvider clock, ILogger<MemoryService> log) : IMemoryService
{
  public async Task<Result<List<MemoryResponse>>> ListAsync(Guid userId, Guid tripId, CancellationToken ct)
  {
    if (await trips.GetOwnedAsync(tripId, userId, ct) is null) return Errors.TripNotFound;
    return (await memories.ListByTripAsync(tripId, ct)).Select(m => m.ToDto(storage)).ToList();
  }

  public async Task<Result<CursorPageResponse<MemoryResponse>>> ListPageAsync(Guid userId, Guid tripId, string? cursor, int? limit, CancellationToken ct)
  {
    if (!CursorPosition.TryDecode(cursor, out var after)) return CursorPagination.InvalidCursor;
    if (await trips.GetOwnedAsync(tripId, userId, ct) is null) return Errors.TripNotFound;
    var page = await memories.ListByTripPageAsync(tripId, after, CursorPagination.NormalizeLimit(limit), ct);
    return new CursorPageResponse<MemoryResponse>(page.Items.Select(m => m.ToDto(storage)).ToList(), page.NextCursor, page.HasMore);
  }

  public async Task<Result<MemoryResponse>> AddAsync(Guid userId, Guid tripId, MemoryRequest req, CancellationToken ct)
  {
    if (await trips.GetOwnedAsync(tripId, userId, ct) is null) return Errors.TripNotFound;
    var draft = req.ToDraft();
    var created = Memory.Create(tripId, draft, clock.GetUtcNow());
    if (created.IsFailure) return created.Error;
    var checkedMedia = await media.CheckAsync(userId, tripId, draft.Type, draft.MediaPath, null, ct);
    if (checkedMedia.IsFailure) return checkedMedia.Error;
    await memories.AddAsync(created.Value, ct);
    await uow.SaveChangesAsync(ct);
    log.LogInformation("Thêm kỷ niệm {MemoryId} ({Type}) vào chuyến {TripId}", created.Value.Id, created.Value.Type, tripId);
    return created.Value.ToDto(storage);
  }

  public async Task<Result<MemoryResponse>> UpdateAsync(Guid userId, Guid memoryId, MemoryRequest req, CancellationToken ct)
  {
    var m = await memories.GetOwnedAsync(memoryId, userId, ct);
    if (m is null) return Errors.MemoryNotFound;
    var draft = req.ToDraft();
    var checkedMedia = await media.CheckAsync(userId, m.TripId, draft.Type, draft.MediaPath, m.MediaPath, ct);
    if (checkedMedia.IsFailure) return checkedMedia.Error;
    var applied = m.ApplyChanges(draft, clock.GetUtcNow());
    if (applied.IsFailure) return applied.Error;
    await uow.SaveChangesAsync(ct);
    return m.ToDto(storage);
  }

  public async Task<Result> DeleteAsync(Guid userId, Guid memoryId, CancellationToken ct)
  {
    var m = await memories.GetOwnedAsync(memoryId, userId, ct);
    if (m is null) return Errors.MemoryNotFound;
    memories.Delete(m);                  // soft delete; the Storage file is kept (only purged later)
    await uow.SaveChangesAsync(ct);
    return Result.Success();
  }
}
