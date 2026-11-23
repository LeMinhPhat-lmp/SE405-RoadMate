using Microsoft.Extensions.Options;
using RoadMate.Application.Abstractions.Persistence;
using RoadMate.Application.Common;
using RoadMate.Application.Shared;
using RoadMate.Application.Tracking;
using RoadMate.Application.Trips.DTOs.Requests;
using RoadMate.Application.Trips.DTOs.Responses;
using RoadMate.Application.Trips.Interfaces;
using RoadMate.Application.Trips.Mappers;
using RoadMate.Domain.Common;
using RoadMate.Domain.Trips;

namespace RoadMate.Application.Trips.Implements;

public class TripService(ITripRepository trips, IEmergencyContactRepository contacts, IUnitOfWork uow, IOptions<TrackingOptions> tracking) : ITripService
{
  public async Task<Result<List<TripListItemResponse>>> ListAsync(Guid userId, string? status, CancellationToken ct)
  {
    TripStatus? filter = null;
    if (status is not null && EnumWire.TryParse<TripStatus>(status, out var s)) filter = s;
    else if (status is not null) return new List<TripListItemResponse>();       // unknown status: legacy returned an empty list
    return (await trips.ListByOwnerAsync(userId, filter, ct)).Select(t => t.ToListItem()).ToList();
  }

  public async Task<Result<CursorPageResponse<TripListItemResponse>>> ListPageAsync(Guid userId, string? status, string? cursor, int? limit, CancellationToken ct)
  {
    if (!CursorPosition.TryDecode(cursor, out var after)) return CursorPagination.InvalidCursor;
    TripStatus? filter = null;
    if (status is not null && EnumWire.TryParse<TripStatus>(status, out var s)) filter = s;
    else if (status is not null) return new CursorPageResponse<TripListItemResponse>([], null, false);
    var page = await trips.ListByOwnerPageAsync(userId, filter, after, CursorPagination.NormalizeLimit(limit), ct);
    return new CursorPageResponse<TripListItemResponse>(page.Items.Select(t => t.ToListItem()).ToList(), page.NextCursor, page.HasMore);
  }

  public async Task<Result<TripResponse>> GetAsync(Guid userId, Guid id, CancellationToken ct)
  {
    var t = await trips.GetOwnedWithDetailsAsync(id, userId, ct);
    return t is null ? Errors.TripNotFound : t.ToDto(tracking.Value.BaseUrl);
  }

  public async Task<Result<TripResponse>> CreateAsync(Guid userId, TripUpsertRequest req, CancellationToken ct)
  {
    var ids = await contacts.ResolveOwnedContactIdsAsync(userId, req.AlertContactIds, ct);
    if (ids.IsFailure) return ids.Error;
    var trip = Trip.Create(userId, req.ToDetails(), req.ToSpecs(), ids.Value);
    if (trip.IsFailure) return trip.Error;
    await trips.AddAsync(trip.Value, ct);
    await uow.SaveChangesAsync(ct);
    return trip.Value.ToDto(tracking.Value.BaseUrl);
  }

  public async Task<Result<TripResponse>> UpdateAsync(Guid userId, Guid id, TripUpsertRequest req, CancellationToken ct)
  {
    var trip = await trips.GetOwnedWithDetailsAsync(id, userId, ct);
    if (trip is null) return Errors.TripNotFound;
    if (trip.Status != TripStatus.Draft) return Errors.TripNotDraft;
    var ids = await contacts.ResolveOwnedContactIdsAsync(userId, req.AlertContactIds, ct);
    if (ids.IsFailure) return ids.Error;
    var applied = trip.UpdateDetails(req.ToDetails());
    if (applied.IsSuccess) applied = trip.ReplaceWaypoints(req.ToSpecs());
    if (applied.IsFailure) return applied.Error;
    trip.SetRecipients(ids.Value);
    await uow.SaveChangesAsync(ct);
    return trip.ToDto(tracking.Value.BaseUrl);
  }

  public async Task<Result> DeleteAsync(Guid userId, Guid id, CancellationToken ct)
  {
    var trip = await trips.GetOwnedAsync(id, userId, ct);
    if (trip is null) return Errors.TripNotFound;
    if (trip.Status == TripStatus.Active) return Error.Conflict("TRIP_ACTIVE", "Hãy kết thúc chuyến trước khi xoá.");
    trips.Delete(trip);
    await uow.SaveChangesAsync(ct);
    return Result.Success();
  }
}
