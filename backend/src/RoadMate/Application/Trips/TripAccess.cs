using RoadMate.Application.Abstractions.Persistence;
using RoadMate.Domain.Common;
using RoadMate.Domain.Trips;

namespace RoadMate.Application.Trips;

public static class TripAccess
{
  /// <summary>Owner's trip that must be Active (GPS, heartbeat, alerts, rest, finish). 404 TRIP_NOT_FOUND / 409 TRIP_NOT_ACTIVE.</summary>
  public static async Task<Result<Trip>> GetOwnedActiveAsync(this ITripRepository trips, Guid tripId, Guid userId, bool withDetails, CancellationToken ct)
  {
    var trip = withDetails ? await trips.GetOwnedWithDetailsAsync(tripId, userId, ct) : await trips.GetOwnedAsync(tripId, userId, ct);
    if (trip is null) return Errors.TripNotFound;
    return trip.Status == TripStatus.Active ? trip : Errors.TripNotActive;
  }

  /// <summary>Every id must be one of the user's (non-deleted) contacts; returns the de-duplicated list.</summary>
  public static async Task<Result<List<Guid>>> ResolveOwnedContactIdsAsync(this IEmergencyContactRepository contacts, Guid userId, IEnumerable<Guid>? ids, CancellationToken ct)
  {
    var distinct = (ids ?? []).Distinct().ToList();
    if (distinct.Count == 0) return distinct;
    var found = await contacts.FilterOwnedIdsAsync(userId, distinct, ct);
    return found.Count == distinct.Count ? distinct : Error.Validation("INVALID_CONTACT", "Có liên hệ không thuộc về bạn.");
  }
}
