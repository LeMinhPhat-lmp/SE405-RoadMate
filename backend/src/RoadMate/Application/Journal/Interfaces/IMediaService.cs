using RoadMate.Application.Journal.DTOs.Requests;
using RoadMate.Application.Journal.DTOs.Responses;
using RoadMate.Domain.Common;

namespace RoadMate.Application.Journal.Interfaces;

public interface IMediaService
{
  /// <summary>Cấp URL PUT đã ký để client upload thẳng lên Storage (server không nhận file).</summary>
  Task<Result<UploadUrlResponse>> CreateUploadUrlAsync(Guid userId, Guid tripId, UploadUrlRequest req, CancellationToken ct);
  /// <summary>Cấp lại URL đọc có hạn cho media của một kỷ niệm (khi URL cũ hết hạn).</summary>
  Task<Result<MediaUrlResponse>> GetDownloadUrlAsync(Guid userId, Guid memoryId, CancellationToken ct);
}
