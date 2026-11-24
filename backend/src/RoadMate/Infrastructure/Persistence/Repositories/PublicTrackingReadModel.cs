using Microsoft.EntityFrameworkCore;
using RoadMate.Application.Abstractions.Persistence;
using RoadMate.Application.Shared;
using RoadMate.Application.Tracking;
using RoadMate.Application.Tracking.DTOs.Responses;
using RoadMate.Domain.Alerts;
using RoadMate.Domain.Common;
using RoadMate.Domain.Trips;

namespace RoadMate.Infrastructure.Persistence.Repositories;

/// <summary>Anonymous relatives' page: only an ABBREVIATED owner name, never ids or contact data.</summary>
public class PublicTrackingReadModel(AppDbContext db) : IPublicTrackingReadModel
{
  public async Task<PublicTrackResponse?> GetAsync(string token, DateTimeOffset now, CancellationToken ct = default)
  {
    var trip = await ActiveByToken(token, now).Include(t => t.Owner).Include(t => t.Live).AsSplitQuery().FirstOrDefaultAsync(ct);
    if (trip is null) return null;
    var open = await db.Alerts.AsNoTracking().Where(a => a.TripId == trip.Id && (a.Status == AlertStatus.Pending || a.Status == AlertStatus.Sent))
      .OrderByDescending(a => a.CreatedAt).ToListAsync(ct);
    var live = trip.Live is null ? null : new PublicLiveResponse(trip.Live.Lat, trip.Live.Lng, trip.Live.Speed, trip.Live.Battery, trip.Live.NetworkOk, trip.Live.UpdatedAt.UtcDateTime);
    return new PublicTrackResponse(NameFormatter.Abbreviate(trip.Owner?.Name), trip.Title, EnumWire.ToWire(trip.Status),
      new LocationResponse(trip.DestName, trip.DestLat, trip.DestLng), live,
      open.Select(a => new PublicAlertResponse(EnumWire.ToWire(a.Type), EnumWire.ToWire(a.Status), a.Lat, a.Lng, a.CreatedAt)).ToList());
  }

  public async Task<PublicRouteResponse?> GetRouteAsync(string token, DateTimeOffset now, int maxPoints, CancellationToken ct = default)
  {
    var trip = await ActiveByToken(token, now).FirstOrDefaultAsync(ct);
    if (trip is null) return null;
    var points = (await db.TripPoints.AsNoTracking().Where(p => p.TripId == trip.Id).OrderByDescending(p => p.RecordedAt).Take(maxPoints).ToListAsync(ct))
      .AsEnumerable().Reverse().Select(p => new LatLng(p.Lat, p.Lng)).ToList();
    return new PublicRouteResponse(trip.PlannedPolyline, points);
  }

  /// <summary>Token must exist and (if the trip ended) not be expired.</summary>
  private IQueryable<Trip> ActiveByToken(string token, DateTimeOffset now) =>
    db.Trips.AsNoTracking().Where(t => t.TrackingToken == token && (t.TrackingExpiresAt == null || t.TrackingExpiresAt > now));
}
