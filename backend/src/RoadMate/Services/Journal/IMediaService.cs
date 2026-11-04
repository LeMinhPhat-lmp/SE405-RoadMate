namespace RoadMate.Services.Journal;

public interface IMediaService
{
  /// <summary>Cấp URL PUT đã ký để client upload thẳng lên Storage (server không nhận file).</summary>
  Task<Result<UploadUrlResponse>> CreateUploadUrlAsync(Guid userId, Guid tripId, UploadUrlRequest req, CancellationToken ct);
  /// <summary>Cấp lại URL đọc có hạn cho media của một kỷ niệm (khi URL cũ hết hạn).</summary>
  Task<Result<MediaUrlResponse>> GetDownloadUrlAsync(Guid userId, Guid memoryId, CancellationToken ct);
}
