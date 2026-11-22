using RoadMate.Domain.Abstractions;
using RoadMate.Domain.Common;

namespace RoadMate.Domain.Trips;

public sealed class Memory : FullAuditedEntity, IAggregateRoot
{
  private Memory() { }

  public Guid TripId { get; private set; }
  public MemoryType Type { get; private set; }
  /// <summary>Object path in the bucket (users/{uid}/trips/{tripId}/{id}.ext); readable URLs are signed on demand.</summary>
  public string? MediaPath { get; private set; }
  public string? Text { get; private set; }
  public double? Lat { get; private set; }
  public double? Lng { get; private set; }
  public string? Mood { get; private set; }
  public string? PlaceName { get; private set; }
  public int? Rating { get; private set; }
  public DateTimeOffset TakenAt { get; private set; }
  public Trip? Trip { get; private set; }

  public static Result<Memory> Create(Guid tripId, MemoryDraft d, DateTimeOffset now)
  {
    var m = new Memory { Id = Guid.NewGuid(), TripId = tripId };
    var r = m.ApplyChanges(d, now);
    return r.IsFailure ? r.Error : m;
  }

  public Result ApplyChanges(MemoryDraft d, DateTimeOffset now)
  {
    if (d.Type == MemoryType.Note && string.IsNullOrWhiteSpace(d.Text)) return Error.Validation("TEXT_REQUIRED", "Ghi chú cần có nội dung.");
    if (d.Type != MemoryType.Note && string.IsNullOrWhiteSpace(d.MediaPath))
      return Error.Validation("MEDIA_REQUIRED", "Ảnh/video cần mediaPath (xin URL bằng POST trips/{id}/media/upload-url rồi upload trước).");
    if (d.Rating is < 1 or > 5) return Error.Validation("INVALID_RATING", "rating từ 1 đến 5.");
    if ((d.Lat is null) != (d.Lng is null) || (d.Lat is not null && !Geo.ValidLatLng(d.Lat.Value, d.Lng!.Value))) return Errors.InvalidLocation;

    Type = d.Type; MediaPath = d.Type == MemoryType.Note ? null : d.MediaPath; Text = d.Text; Lat = d.Lat; Lng = d.Lng;
    Mood = d.Mood; PlaceName = d.PlaceName; Rating = d.Rating; TakenAt = d.TakenAt ?? now;
    return Result.Success();
  }
}
