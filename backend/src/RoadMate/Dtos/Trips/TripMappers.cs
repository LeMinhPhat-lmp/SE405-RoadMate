namespace RoadMate.Dtos.Trips;

public static class TripMappers
{
  public static WaypointDto ToDto(this TripWaypoint w) => new(w.Seq, w.Name, w.Lat, w.Lng, w.PlannedStayMin);

  public static TripStatsDto ToDto(this TripStats s) => new(s.DistanceKm, s.MovingMin, s.RestMin, s.StopCount, s.MaxSpeed, s.AvgSpeed, s.MaxAltitude, s.AlertCount);

  public static TripDto ToDto(this Trip t, string trackingBase) => new(t.Id, t.Title, t.Status,
    new LocationDto(t.OriginName, t.OriginLat, t.OriginLng), new LocationDto(t.DestName, t.DestLat, t.DestLng),
    t.Waypoints.OrderBy(w => w.Seq).Select(w => w.ToDto()).ToList(), t.PlannedPolyline, t.DistanceKm, t.EtaMinutes,
    t.AvoidHighway, t.AvoidToll, t.StartTime, t.EndTime, t.ExpectedReturnTime, t.IdleMinutes, t.CrashSensitivity, t.LiveShare,
    TrackingUrl.Build(trackingBase, t.TrackingToken), t.RestUntil, t.Recipients.Select(r => r.ContactId).ToList());

  public static TripListItemDto ToListItem(this Trip t) => new(t.Id, t.Title, t.Status, t.OriginName, t.DestName, t.DistanceKm, t.StartTime, t.EndTime, t.CreatedAt);

  /// <summary>Chép nội dung request vào chuyến (thay toàn bộ waypoint theo thứ tự gửi lên).</summary>
  public static void ApplyFrom(this Trip t, TripUpsertRequest r)
  {
    t.Title = r.Title.Trim();
    t.OriginName = r.Origin.Name; t.OriginLat = r.Origin.Lat; t.OriginLng = r.Origin.Lng;
    t.DestName = r.Destination.Name; t.DestLat = r.Destination.Lat; t.DestLng = r.Destination.Lng;
    t.PlannedPolyline = r.PlannedPolyline; t.DistanceKm = r.DistanceKm ?? 0; t.EtaMinutes = r.EtaMinutes ?? 0;
    t.AvoidHighway = r.AvoidHighway; t.AvoidToll = r.AvoidToll;
    t.ExpectedReturnTime = r.ExpectedReturnTime.ToUtc();
    t.IdleMinutes = r.IdleMinutes; t.CrashSensitivity = r.CrashSensitivity; t.LiveShare = r.LiveShare;

    t.Waypoints.Clear();
    int seq = 0;
    foreach (var w in r.Waypoints ?? new List<WaypointInput>())
      t.Waypoints.Add(new TripWaypoint { Id = Guid.NewGuid(), TripId = t.Id, Seq = seq++, Name = w.Name, Lat = w.Lat, Lng = w.Lng, PlannedStayMin = w.PlannedStayMin });
  }
}
