using RoadMate.Application.Abstractions.Storage;
using RoadMate.Application.Common;
using RoadMate.Application.Journal.DTOs.Requests;
using RoadMate.Application.Journal.DTOs.Responses;
using RoadMate.Domain.Common;
using RoadMate.Domain.Trips;

namespace RoadMate.Application.Journal.Mappers;

public static class MemoryMappers
{
  /// <summary>Adds a signed read URL when the memory has media and Storage is configured (signing is local, no network call).</summary>
  public static MemoryResponse ToDto(this Memory m, IMediaStorage storage)
  {
    SignedUrl? signed = null;
    if (!string.IsNullOrEmpty(m.MediaPath) && storage.CreateDownloadUrl(m.MediaPath) is { IsSuccess: true } r) signed = r.Value;
    return new(m.Id, EnumWire.ToWire(m.Type), m.MediaPath, signed?.Url, signed?.ExpiresAt, m.Text, m.Lat, m.Lng, m.Mood, m.PlaceName, m.Rating, m.TakenAt.ToWire());
  }

  /// <summary>The request type was validated (INVALID_TYPE) before this point.</summary>
  public static MemoryDraft ToDraft(this MemoryRequest r)
  {
    EnumWire.TryParse<MemoryType>(r.Type, out var type);
    return new MemoryDraft(type, r.MediaPath, r.Text, r.Lat, r.Lng, r.Mood, r.PlaceName, r.Rating, r.TakenAt.ToUtcOffset());
  }
}
