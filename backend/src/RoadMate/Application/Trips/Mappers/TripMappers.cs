using RoadMate.Application.Common;
using RoadMate.Application.Shared;
using RoadMate.Application.Trips.DTOs.Requests;
using RoadMate.Application.Trips.DTOs.Responses;
using RoadMate.Domain.Common;
using RoadMate.Domain.Trips;
using RoadMate.Domain.ValueObjects;

namespace RoadMate.Application.Trips.Mappers;

public static class TripMappers
{
  public static WaypointResponse ToDto(this TripWaypoint w) => new(w.Seq, w.Name, w.Lat, w.Lng, w.PlannedStayMin);
  public static TripStatsResponse ToDto(this TripStats s) => new(s.DistanceKm, s.MovingMin, s.RestMin, s.StopCount, s.MaxSpeed, s.AvgSpeed, s.MaxAltitude, s.AlertCount);

  public static TripResponse ToDto(this Trip t, string trackingBase) => new(t.Id, t.Title, EnumWire.ToWire(t.Status),
    new LocationResponse(t.OriginName, t.OriginLat, t.OriginLng), new LocationResponse(t.DestName, t.DestLat, t.DestLng),
    t.Waypoints.OrderBy(w => w.Seq).Select(w => w.ToDto()).ToList(), t.PlannedPolyline, t.DistanceKm, t.EtaMinutes,
    t.AvoidHighway, t.AvoidToll, t.StartTime.ToWire(), t.EndTime.ToWire(), t.ExpectedReturnTime.ToWire(), t.IdleMinutes,
    t.CrashSensitivity is { } cs ? EnumWire.ToWire(cs) : null, t.LiveShare,
    TrackingUrl.Build(trackingBase, t.TrackingToken), t.RestUntil.ToWire(), t.Recipients.Select(r => r.ContactId).ToList(), t.GroupId);

  public static TripListItemResponse ToListItem(this Trip t) =>
    new(t.Id, t.Title, EnumWire.ToWire(t.Status), t.OriginName, t.DestName, t.DistanceKm, t.StartTime.ToWire(), t.EndTime.ToWire(), t.CreatedAt);

  /// <summary>Request → domain input. The request was validated (coordinates, sensitivity), so parsing cannot fail here.</summary>
  public static TripDetails ToDetails(this TripUpsertRequest r) => new(
    r.Title, r.Origin.Name, GeoPoint.Create(r.Origin.Lat, r.Origin.Lng).Value, r.Destination.Name, GeoPoint.Create(r.Destination.Lat, r.Destination.Lng).Value,
    r.PlannedPolyline, r.DistanceKm ?? 0, r.EtaMinutes ?? 0, r.AvoidHighway, r.AvoidToll, r.ExpectedReturnTime.ToUtcOffset(), r.IdleMinutes,
    EnumWire.TryParse<RoadMate.Domain.Users.CrashSensitivity>(r.CrashSensitivity, out var s) ? s : null, r.LiveShare);

  public static List<WaypointSpec> ToSpecs(this TripUpsertRequest r) =>
    (r.Waypoints ?? []).Select(w => new WaypointSpec(w.Name, GeoPoint.Create(w.Lat, w.Lng).Value, w.PlannedStayMin)).ToList();
}
