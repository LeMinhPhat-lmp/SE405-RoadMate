using RoadMate.Application.Abstractions.Persistence;
using RoadMate.Application.Alerts.DTOs.Requests;
using RoadMate.Application.Alerts.DTOs.Responses;
using RoadMate.Application.Alerts.Interfaces;
using RoadMate.Application.Alerts.Mappers;
using RoadMate.Application.Common;
using RoadMate.Application.Shared;
using RoadMate.Application.Trips;
using RoadMate.Domain.Alerts;
using RoadMate.Domain.Common;
using RoadMate.Domain.ValueObjects;

namespace RoadMate.Application.Alerts.Implements;

public class AlertService(ITripRepository trips, IAlertRaiser raiser, IAlertReadModel read, TimeProvider clock) : IAlertService
{
  public async Task<Result<AlertResponse>> CreateAsync(Guid userId, Guid tripId, CreateAlertRequest req, CancellationToken ct)
  {
    // Request shape (clientId/type/coordinates) was validated by the request validator.
    var trip = await trips.GetOwnedActiveAsync(tripId, userId, false, ct);
    if (trip.IsFailure) return trip.Error;
    EnumWire.TryParse<AlertType>(req.Type, out var type);
    var cmd = new RaiseAlertCommand(tripId, userId, ClientRequestId.Create(req.ClientId).Value, type, GeoPoint.Create(req.Lat, req.Lng).Value, req.Battery, null);
    var raised = await raiser.RaiseAsync(cmd, clock.GetUtcNow(), ct);
    if (raised.IsFailure) return raised.Error;
    return raised.Value.Created
      ? raised.Value.Alert.ToDto(raised.Value.Plan.Deliveries, raised.Value.Plan.Acks)
      : await ExistingAsync(raised.Value.Alert, ct);
  }

  public async Task<Result<List<AlertResponse>>> ListAsync(Guid userId, Guid tripId, CancellationToken ct)
  {
    var trip = await trips.GetOwnedAsync(tripId, userId, ct);
    if (trip is null) return Errors.TripNotFound;
    return (await read.ListForTripAsync(tripId, ct)).ToList();
  }

  public async Task<Result<CursorPageResponse<AlertResponse>>> ListPageAsync(Guid userId, Guid tripId, string? cursor, int? limit, CancellationToken ct)
  {
    if (!CursorPosition.TryDecode(cursor, out var after)) return CursorPagination.InvalidCursor;
    var trip = await trips.GetOwnedAsync(tripId, userId, ct);
    if (trip is null) return Errors.TripNotFound;
    var page = await read.ListForTripPageAsync(tripId, after, CursorPagination.NormalizeLimit(limit), ct);
    return new CursorPageResponse<AlertResponse>(page.Items, page.NextCursor, page.HasMore);
  }

  private async Task<AlertResponse> ExistingAsync(Alert a, CancellationToken ct) =>
    (await read.ListForTripAsync(a.TripId, ct)).FirstOrDefault(x => x.Id == a.Id) ?? a.ToDto([], []);
}
