using RoadMate.Infrastructure.Storage;

namespace RoadMate.Services.Journal;

public class MemoryService(IUnitOfWork uow, IMediaStorage storage, IOptions<StorageOptions> storageOptions, TimeProvider clock,
  ILogger<MemoryService> log) : IMemoryService
{
  public async Task<Result<List<MemoryDto>>> ListAsync(Guid userId, Guid tripId, CancellationToken ct)
  {
    if (await uow.TripRepository.GetOwnedAsync(tripId, userId, ct) is null) return Errors.TripNotFound;
    return (await uow.MemoryRepository.ListByTripAsync(tripId, ct)).Select(m => m.ToDto(storage)).ToList();
  }

  public async Task<Result<MemoryDto>> AddAsync(Guid userId, Guid tripId, MemoryRequest req, CancellationToken ct)
  {
    if (await uow.TripRepository.GetOwnedAsync(tripId, userId, ct) is null) return Errors.TripNotFound;
    var valid = MemoryRequestValidator.Validate(req);
    if (valid.IsFailure) return valid.Error;
    var media = await ValidateMediaAsync(userId, tripId, req, currentPath: null, ct);
    if (media.IsFailure) return media.Error;

    var m = new Memory { Id = Guid.NewGuid(), TripId = tripId };
    m.ApplyFrom(req, clock.UtcNow());
    await uow.MemoryRepository.AddAsync(m, ct);
    await uow.SaveChangesAsync(ct);
    log.LogInformation("Thêm kỷ niệm {MemoryId} ({Type}) vào chuyến {TripId}", m.Id, m.Type, tripId);
    return m.ToDto(storage);
  }

  public async Task<Result<MemoryDto>> UpdateAsync(Guid userId, Guid memoryId, MemoryRequest req, CancellationToken ct)
  {
    var m = await uow.MemoryRepository.GetOwnedAsync(memoryId, userId, ct);
    if (m is null) return Errors.MemoryNotFound;
    var valid = MemoryRequestValidator.Validate(req);
    if (valid.IsFailure) return valid.Error;
    var media = await ValidateMediaAsync(userId, m.TripId, req, currentPath: m.MediaPath, ct);
    if (media.IsFailure) return media.Error;

    m.ApplyFrom(req, clock.UtcNow());
    await uow.SaveChangesAsync(ct);
    return m.ToDto(storage);
  }

  public async Task<Result> DeleteAsync(Guid userId, Guid memoryId, CancellationToken ct)
  {
    var m = await uow.MemoryRepository.GetOwnedAsync(memoryId, userId, ct);
    if (m is null) return Errors.MemoryNotFound;
    uow.MemoryRepository.Delete(m);                    // xoá mềm; file trên Storage được giữ lại (chỉ xoá khi purge)
    await uow.SaveChangesAsync(ct);
    return Result.Success();
  }

  /// <summary>
  /// mediaPath phải thuộc đúng user + chuyến; nếu Storage:VerifyOnCreate thì object phải thực sự tồn tại, đúng loại và không vượt dung lượng.
  /// Bỏ qua khi là ghi chú hoặc mediaPath không đổi so với bản đang lưu.
  /// </summary>
  private async Task<Result> ValidateMediaAsync(Guid userId, Guid tripId, MemoryRequest req, string? currentPath, CancellationToken ct)
  {
    if (req.Type == MemoryType.Note || string.IsNullOrEmpty(req.MediaPath) || req.MediaPath == currentPath) return Result.Success();

    var user = await uow.UserRepository.GetByIdAsync(userId, false, ct);
    if (user is null) return Errors.UserNotSynced;
    if (!MediaPaths.IsOwnedBy(req.MediaPath, user.FirebaseUid, tripId))
    {
      log.LogWarning("User {UserId} gửi mediaPath không thuộc chuyến {TripId}", userId, tripId);
      return Error.Validation("INVALID_MEDIA_PATH", "mediaPath không thuộc chuyến đi/tài khoản của bạn.");
    }

    var o = storageOptions.Value;
    if (!o.VerifyOnCreate) return Result.Success();
    var stored = await storage.GetObjectAsync(req.MediaPath, ct);
    if (stored.IsFailure) return stored.Error;
    if (stored.Value.SizeBytes > MediaPolicy.MaxBytes(req.Type, o)) return Error.Validation("FILE_TOO_LARGE", "File đã upload vượt dung lượng cho phép.");
    if (!MediaPolicy.TryGetExtension(req.Type, (stored.Value.ContentType ?? "").ToLowerInvariant(), out _))
      return Error.Validation("UNSUPPORTED_CONTENT_TYPE", "File đã upload không đúng loại ảnh/video cho phép.");
    return Result.Success();
  }
}
