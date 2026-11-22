using RoadMate.Domain.Common;

namespace RoadMate.Application.Abstractions.Storage;

/// <summary>
/// Cổng duy nhất tới Firebase Storage (GCS) cho media: ký URL (pre-signed, V4) để client upload/đọc trực tiếp,
/// server không phải truyền tải file. Chữ ký tạo cục bộ bằng private key của service account (không gọi mạng).
/// </summary>
public interface IMediaStorage
{
  /// <summary>URL PUT để upload đúng <paramref name="contentType"/> (và tối đa <paramref name="maxBytes"/> nếu bật EnforceSizeLimit).</summary>
  Result<SignedUpload> CreateUploadUrl(string objectPath, string contentType, long maxBytes);

  /// <summary>URL GET có hạn để đọc object.</summary>
  Result<SignedUrl> CreateDownloadUrl(string objectPath);

  /// <summary>Metadata object (kích thước, content type); NotFound nếu chưa upload.</summary>
  Task<Result<StoredObject>> GetObjectAsync(string objectPath, CancellationToken ct);
}
