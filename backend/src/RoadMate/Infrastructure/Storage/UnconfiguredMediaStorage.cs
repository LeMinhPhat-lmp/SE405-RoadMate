namespace RoadMate.Infrastructure.Storage;

/// <summary>Dùng khi chưa nạp được service account: mọi thao tác media trả 503 STORAGE_NOT_CONFIGURED.</summary>
public class UnconfiguredMediaStorage : IMediaStorage
{
  public static readonly Error NotConfigured = Error.NotConfigured("STORAGE_NOT_CONFIGURED",
    "Chưa cấu hình Firebase Storage (Firebase:CredentialPath trỏ tới service account hợp lệ và Storage:Bucket).");

  public Result<SignedUpload> CreateUploadUrl(string objectPath, string contentType, long maxBytes) => NotConfigured;
  public Result<SignedUrl> CreateDownloadUrl(string objectPath) => NotConfigured;
  public Task<Result<StoredObject>> GetObjectAsync(string objectPath, CancellationToken ct) => Task.FromResult<Result<StoredObject>>(NotConfigured);
}
