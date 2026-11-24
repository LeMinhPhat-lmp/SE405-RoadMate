using Microsoft.Extensions.Options;
using RoadMate.Application.Abstractions.Persistence;
using RoadMate.Application.Abstractions.Storage;
using RoadMate.Application.Journal.Interfaces;
using RoadMate.Domain.Common;
using RoadMate.Domain.Trips;

namespace RoadMate.Application.Journal.Implements;

public class MediaReferenceChecker(IUserRepository users, IMediaStorage storage, IOptions<StorageOptions> options, ILogger<MediaReferenceChecker> log) : IMediaReferenceChecker
{
  public async Task<Result> CheckAsync(Guid userId, Guid tripId, MemoryType type, string? mediaPath, string? currentPath, CancellationToken ct)
  {
    if (type == MemoryType.Note || string.IsNullOrEmpty(mediaPath) || mediaPath == currentPath) return Result.Success();
    var user = await users.GetByIdAsync(userId, cancellationToken: ct);
    if (user is null) return Errors.UserNotSynced;
    if (!MediaPaths.IsOwnedBy(mediaPath, user.FirebaseUid, tripId))
    {
      log.LogWarning("User {UserId} gửi mediaPath không thuộc chuyến {TripId}", userId, tripId);
      return Error.Validation("INVALID_MEDIA_PATH", "mediaPath không thuộc chuyến đi/tài khoản của bạn.");
    }
    var o = options.Value;
    if (!o.VerifyOnCreate) return Result.Success();
    var kind = EnumWire.ToWire(type);
    var stored = await storage.GetObjectAsync(mediaPath, ct);
    if (stored.IsFailure) return stored.Error;
    if (stored.Value.SizeBytes > MediaPolicy.MaxBytes(kind, o)) return Error.Validation("FILE_TOO_LARGE", "File đã upload vượt dung lượng cho phép.");
    if (!MediaPolicy.TryGetExtension(kind, (stored.Value.ContentType ?? "").ToLowerInvariant(), out _))
      return Error.Validation("UNSUPPORTED_CONTENT_TYPE", "File đã upload không đúng loại ảnh/video cho phép.");
    return Result.Success();
  }
}
