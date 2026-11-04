namespace RoadMate.Services.Journal;

public static class MemoryRequestValidator
{
  public static Result Validate(MemoryRequest r)
  {
    if (!MemoryType.All.Contains(r.Type)) return Error.Validation("INVALID_TYPE", "type phải là photo|note|video.");
    if (r.Type == MemoryType.Note && string.IsNullOrWhiteSpace(r.Text)) return Error.Validation("TEXT_REQUIRED", "Ghi chú cần có nội dung.");
    if (r.Type != MemoryType.Note && string.IsNullOrWhiteSpace(r.MediaPath))
      return Error.Validation("MEDIA_REQUIRED", "Ảnh/video cần mediaPath (xin URL bằng POST trips/{id}/media/upload-url rồi upload trước).");
    if (r.Rating is < 1 or > 5) return Error.Validation("INVALID_RATING", "rating từ 1 đến 5.");
    if ((r.Lat is null) != (r.Lng is null) || (r.Lat is not null && !Geo.ValidLatLng(r.Lat.Value, r.Lng!.Value))) return Errors.InvalidLocation;
    return Result.Success();
  }
}
