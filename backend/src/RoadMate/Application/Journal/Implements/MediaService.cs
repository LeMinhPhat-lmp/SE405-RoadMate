using Microsoft.Extensions.Options;
using RoadMate.Application.Abstractions.Persistence;
using RoadMate.Application.Abstractions.Storage;
using RoadMate.Application.Journal.DTOs.Requests;
using RoadMate.Application.Journal.DTOs.Responses;
using RoadMate.Application.Journal.Interfaces;
using RoadMate.Domain.Common;

namespace RoadMate.Application.Journal.Implements;

public class MediaService(ITripRepository trips, IUserRepository users, IMemoryRepository memories, IMediaStorage storage, IOptions<StorageOptions> options) : IMediaService
{
  public async Task<Result<UploadUrlResponse>> CreateUploadUrlAsync(Guid userId, Guid tripId, UploadUrlRequest req, CancellationToken ct)
  {
    if (await trips.GetOwnedAsync(tripId, userId, ct) is null) return Errors.TripNotFound;
    var user = await users.GetByIdAsync(userId, cancellationToken: ct);
    if (user is null) return Errors.UserNotSynced;
    var kind = (req.Kind ?? "").Trim().ToLowerInvariant();
    var contentType = (req.ContentType ?? "").Trim().ToLowerInvariant();
    if (!MediaPolicy.TryGetExtension(kind, contentType, out var extension))
      return Error.Validation("UNSUPPORTED_CONTENT_TYPE", $"contentType '{contentType}' không được hỗ trợ cho {kind}.");
    var max = MediaPolicy.MaxBytes(kind, options.Value);
    var path = MediaPaths.Build(user.FirebaseUid, tripId, extension);
    var signed = storage.CreateUploadUrl(path, contentType, max);
    if (signed.IsFailure) return signed.Error;
    return new UploadUrlResponse(path, signed.Value.Url, "PUT", signed.Value.Headers, signed.Value.ExpiresAt, max);
  }

  public async Task<Result<MediaUrlResponse>> GetDownloadUrlAsync(Guid userId, Guid memoryId, CancellationToken ct)
  {
    var memory = await memories.GetOwnedAsync(memoryId, userId, ct);
    if (memory is null) return Errors.MemoryNotFound;
    if (string.IsNullOrEmpty(memory.MediaPath)) return Error.NotFound("NO_MEDIA", "Kỷ niệm này không có ảnh/video.");
    var signed = storage.CreateDownloadUrl(memory.MediaPath);
    return signed.IsFailure ? signed.Error : new MediaUrlResponse(signed.Value.Url, signed.Value.ExpiresAt);
  }
}
