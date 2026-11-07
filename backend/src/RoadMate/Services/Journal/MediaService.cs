using RoadMate.Infrastructure.Storage;

namespace RoadMate.Services.Journal;

public class MediaService(IUnitOfWork uow, IMediaStorage storage, IOptions<StorageOptions> options) : IMediaService
{
  public async Task<Result<UploadUrlResponse>> CreateUploadUrlAsync(Guid userId, Guid tripId, UploadUrlRequest req, CancellationToken ct)
  {
    if (await uow.TripRepository.GetOwnedAsync(tripId, userId, ct) is null) return Errors.TripNotFound;
    var user = await uow.UserRepository.GetByIdAsync(userId, false, ct);
    if (user is null) return Errors.UserNotSynced;

    var kind = (req.Kind ?? "").Trim().ToLowerInvariant();
    if (!MediaPolicy.IsMediaKind(kind)) return Error.Validation("INVALID_KIND", "kind phải là photo|video.");
    var contentType = (req.ContentType ?? "").Trim().ToLowerInvariant();
    if (!MediaPolicy.TryGetExtension(kind, contentType, out var extension))
      return Error.Validation("UNSUPPORTED_CONTENT_TYPE", $"contentType '{contentType}' không được hỗ trợ cho {kind}.");
    var max = MediaPolicy.MaxBytes(kind, options.Value);
    if (req.SizeBytes <= 0 || req.SizeBytes > max) return Error.Validation("FILE_TOO_LARGE", $"Dung lượng phải từ 1 đến {max} byte.");

    var path = MediaPaths.Build(user.FirebaseUid, tripId, extension);
    var signed = storage.CreateUploadUrl(path, contentType, max);
    if (signed.IsFailure) return signed.Error;
    return new UploadUrlResponse(path, signed.Value.Url, "PUT", signed.Value.Headers, signed.Value.ExpiresAt, max);
  }

  public async Task<Result<MediaUrlResponse>> GetDownloadUrlAsync(Guid userId, Guid memoryId, CancellationToken ct)
  {
    var memory = await uow.MemoryRepository.GetOwnedAsync(memoryId, userId, ct);
    if (memory is null) return Errors.MemoryNotFound;
    if (string.IsNullOrEmpty(memory.MediaPath)) return Error.NotFound("NO_MEDIA", "Kỷ niệm này không có ảnh/video.");
    var signed = storage.CreateDownloadUrl(memory.MediaPath);
    return signed.IsFailure ? signed.Error : new MediaUrlResponse(signed.Value.Url, signed.Value.ExpiresAt);
  }
}
