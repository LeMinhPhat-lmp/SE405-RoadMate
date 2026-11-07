using RoadMate.Infrastructure.Storage;

namespace RoadMate.Dtos.Journal;

public static class MemoryMappers
{
  /// <summary>Kèm URL đọc đã ký (nếu có media và Storage đã cấu hình). Ký là phép tính cục bộ, không gọi mạng.</summary>
  public static MemoryDto ToDto(this Memory m, IMediaStorage storage)
  {
    SignedUrl? signed = null;
    if (!string.IsNullOrEmpty(m.MediaPath) && storage.CreateDownloadUrl(m.MediaPath) is { IsSuccess: true } r) signed = r.Value;
    return new(m.Id, m.Type, m.MediaPath, signed?.Url, signed?.ExpiresAt, m.Text, m.Lat, m.Lng, m.Mood, m.PlaceName, m.Rating, m.TakenAt);
  }

  public static void ApplyFrom(this Memory m, MemoryRequest r, DateTime now)
  {
    m.Type = r.Type; m.MediaPath = r.Type == MemoryType.Note ? null : r.MediaPath; m.Text = r.Text; m.Lat = r.Lat; m.Lng = r.Lng;
    m.Mood = r.Mood; m.PlaceName = r.PlaceName; m.Rating = r.Rating; m.TakenAt = (r.TakenAt ?? now).ToUtc();
  }
}
