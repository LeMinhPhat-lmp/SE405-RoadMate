namespace RoadMate.Infrastructure.Storage;

public record SignedUrl(string Url, DateTime ExpiresAt);

/// <param name="Headers">Header client PHẢI gửi y nguyên khi PUT (nằm trong chữ ký), nếu không GCS trả 403.</param>
public record SignedUpload(string Url, IReadOnlyDictionary<string, string> Headers, DateTime ExpiresAt);

public record StoredObject(long SizeBytes, string? ContentType);

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
