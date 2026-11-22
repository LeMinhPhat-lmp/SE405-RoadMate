using RoadMate.Application.Accounts.Validators;
using RoadMate.Application.Abstractions.Messaging;
using RoadMate.Application.Accounts;
using RoadMate.Application.Trips.DTOs.Requests;
using RoadMate.Domain.Common;

namespace RoadMate.Application.Trips.Validators;

/// <summary>Order identical to the legacy validator; the waypoint cap now also lives in the aggregate.</summary>
public sealed class TripUpsertRequestValidator : IRequestValidator<TripUpsertRequest>
{
  public Result Validate(TripUpsertRequest r, DateTimeOffset now)
  {
    if (string.IsNullOrWhiteSpace(r.Title) || r.Title.Length > 200) return Error.Validation("INVALID_TITLE", "Tên chuyến bắt buộc (≤200 ký tự).");
    if (!Geo.ValidLatLng(r.Origin.Lat, r.Origin.Lng) || !Geo.ValidLatLng(r.Destination.Lat, r.Destination.Lng)) return Errors.InvalidLocation;
    if ((r.Waypoints?.Count ?? 0) > Errors.MaxWaypoints) return Errors.TooManyWaypoints;
    if (r.Waypoints?.Any(w => !Geo.ValidLatLng(w.Lat, w.Lng)) == true) return Errors.InvalidLocation;
    return SafetySettingsRules.Check(r.CrashSensitivity, r.IdleMinutes);
  }
}
