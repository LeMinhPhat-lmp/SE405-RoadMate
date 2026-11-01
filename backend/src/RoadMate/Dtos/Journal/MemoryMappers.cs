namespace RoadMate.Dtos.Journal;

public static class MemoryMappers
{
  public static MemoryDto ToDto(this Memory m) => new(m.Id, m.Type, m.MediaUrl, m.Text, m.Lat, m.Lng, m.Mood, m.PlaceName, m.Rating, m.TakenAt);

  public static void ApplyFrom(this Memory m, MemoryRequest r)
  {
    m.Type = r.Type; m.MediaUrl = r.MediaUrl; m.Text = r.Text; m.Lat = r.Lat; m.Lng = r.Lng;
    m.Mood = r.Mood; m.PlaceName = r.PlaceName; m.Rating = r.Rating; m.TakenAt = (r.TakenAt ?? DateTime.UtcNow).ToUtc();
  }
}
