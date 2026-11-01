namespace RoadMate.Services.Trips;

public static class TripUpsertRequestValidator
{
  public const int MaxWaypoints = 20;

  public static Result Validate(TripUpsertRequest r)
  {
    if (string.IsNullOrWhiteSpace(r.Title) || r.Title.Length > 200) return Error.Validation("INVALID_TITLE", "Tên chuyến bắt buộc (≤200 ký tự).");
    if (!Geo.ValidLatLng(r.Origin.Lat, r.Origin.Lng) || !Geo.ValidLatLng(r.Destination.Lat, r.Destination.Lng))
      return Error.Validation("INVALID_LOCATION", "Toạ độ điểm đi/đến không hợp lệ.");
    if ((r.Waypoints?.Count ?? 0) > MaxWaypoints) return Error.Validation("TOO_MANY_WAYPOINTS", $"Tối đa {MaxWaypoints} điểm dừng.");
    if (r.Waypoints?.Any(w => !Geo.ValidLatLng(w.Lat, w.Lng)) == true) return Error.Validation("INVALID_LOCATION", "Toạ độ điểm dừng không hợp lệ.");
    return SafetySettingsValidator.Validate(r.CrashSensitivity, r.IdleMinutes);
  }
}
