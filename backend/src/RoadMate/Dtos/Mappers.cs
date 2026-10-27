namespace RoadMate.Dtos;

public static class Mappers
{
  public static UserDto ToDto(this User u) => new(u.Id, u.Name, u.Email, u.Phone, u.AvatarUrl, u.BloodType, u.MedicalNotes,
    u.VehicleType, u.PlateNumber, u.IdleMinutes, u.CrashSensitivity, u.Language, u.Theme);

  public static ContactDto ToDto(this EmergencyContact c) => new(c.Id, c.Name, c.Phone, c.Relation, c.Priority, c.LinkedUserId != null);

  public static TripStatsDto ToDto(this TripStats s) => new(s.DistanceKm, s.MovingMin, s.RestMin, s.StopCount, s.MaxSpeed, s.AvgSpeed, s.MaxAltitude, s.AlertCount);

  public static WaypointDto ToDto(this TripWaypoint w) => new(w.Seq, w.Name, w.Lat, w.Lng, w.PlannedStayMin);

  public static TripDto ToDto(this Trip t, string trackingBase) => new(t.Id, t.Title, t.Status,
    new LocationDto(t.OriginName, t.OriginLat, t.OriginLng), new LocationDto(t.DestName, t.DestLat, t.DestLng),
    t.Waypoints.OrderBy(w => w.Seq).Select(w => w.ToDto()).ToList(), t.PlannedPolyline, t.DistanceKm, t.EtaMinutes,
    t.AvoidHighway, t.AvoidToll, t.StartTime, t.EndTime, t.ExpectedReturnTime, t.IdleMinutes, t.CrashSensitivity, t.LiveShare,
    TrackingUrl(t, trackingBase), t.RestUntil, t.Recipients.Select(r => r.ContactId).ToList());

  public static string? TrackingUrl(Trip t, string baseUrl) =>
    string.IsNullOrEmpty(t.TrackingToken) ? null : $"{baseUrl.TrimEnd('/')}/{t.TrackingToken}";

  public static TripListItemDto ToListItem(this Trip t) => new(t.Id, t.Title, t.Status, t.OriginName, t.DestName, t.DistanceKm, t.StartTime, t.EndTime, t.CreatedAt);

  public static MemoryDto ToDto(this Memory m) => new(m.Id, m.Type, m.MediaUrl, m.Text, m.Lat, m.Lng, m.Mood, m.PlaceName, m.Rating, m.TakenAt);

  public static AlertDto ToDto(this Alert a, bool withDeliveries = true) => new(a.Id, a.TripId, a.Type, a.Status, a.Lat, a.Lng, a.Battery,
   a.RepeatCount, a.CreatedAt, a.ResolvedAt, a.ResolvedBy,
   withDeliveries ? a.Deliveries.Select(d => new DeliveryDto(d.ContactId, d.Channel, d.Status, d.Error)).ToList() : null);
}
