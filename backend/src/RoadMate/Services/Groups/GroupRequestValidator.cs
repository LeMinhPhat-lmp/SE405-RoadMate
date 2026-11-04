namespace RoadMate.Services.Groups;

public static class GroupRequestValidator
{
  public const int MaxNameLength = 100;

  public static Result ValidateCreate(CreateGroupRequest r, DateTime now)
  {
    if (string.IsNullOrWhiteSpace(r.Name) || r.Name.Trim().Length > MaxNameLength)
      return Error.Validation("INVALID_NAME", $"Tên nhóm bắt buộc, tối đa {MaxNameLength} ký tự.");
    return ValidateMeeting(r.MeetingLat, r.MeetingLng, r.DepartAt, now);
  }

  /// <summary>Toạ độ phải đi cùng nhau và hợp lệ; giờ xuất phát (nếu có) phải ở tương lai.</summary>
  public static Result ValidateMeeting(double? lat, double? lng, DateTime? departAt, DateTime now)
  {
    if ((lat is null) != (lng is null) || (lat is not null && !Geo.ValidLatLng(lat.Value, lng!.Value))) return Errors.InvalidLocation;
    if (departAt is not null && departAt.Value.ToUtc() <= now) return Error.Validation("INVALID_DEPART_TIME", "Giờ xuất phát phải ở tương lai.");
    return Result.Success();
  }
}
