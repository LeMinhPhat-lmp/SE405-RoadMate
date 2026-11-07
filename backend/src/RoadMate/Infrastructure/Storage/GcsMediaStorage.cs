using Google;
using Google.Apis.Auth.OAuth2;
using Google.Cloud.Storage.V1;

namespace RoadMate.Infrastructure.Storage;

/// <summary>
/// Pre-signed URL V4 cho bucket Firebase Storage. Lưu ý: URL ký đi qua GCS (storage.googleapis.com/{bucket}/{object}),
/// quyền kiểm soát bằng IAM của service account – Firebase Storage Security Rules KHÔNG áp dụng cho URL này.
/// </summary>
public class GcsMediaStorage(ServiceAccountCredential credential, StorageClient client, IOptions<StorageOptions> options,
  TimeProvider clock, ILogger<GcsMediaStorage> log) : IMediaStorage
{
  private readonly UrlSigner _signer = UrlSigner.FromCredential(credential);
  private const string SizeRangeHeader = "x-goog-content-length-range";

  public Result<SignedUpload> CreateUploadUrl(string objectPath, string contentType, long maxBytes)
  {
    var o = options.Value;
    var duration = TimeSpan.FromMinutes(Math.Clamp(o.UploadUrlMinutes, 1, 60));
    var headers = new Dictionary<string, string> { ["Content-Type"] = contentType };
    var request = new Dictionary<string, IEnumerable<string>>();
    if (o.EnforceSizeLimit)
    {
      var range = $"0,{maxBytes}";
      headers[SizeRangeHeader] = range;
      request[SizeRangeHeader] = [range];
    }

    try
    {
      var template = UrlSigner.RequestTemplate
        .FromBucket(o.Bucket).WithObjectName(objectPath).WithHttpMethod(HttpMethod.Put)
        .WithContentHeaders(new Dictionary<string, IEnumerable<string>> { ["Content-Type"] = [contentType] });
      if (request.Count > 0) template = template.WithRequestHeaders(request);

      var url = _signer.Sign(template, UrlSigner.Options.FromDuration(duration).WithSigningVersion(SigningVersion.V4));
      log.LogInformation("Đã ký URL upload {ObjectPath} ({ContentType}, tối đa {MaxBytes}B, hạn {Minutes} phút)", objectPath, contentType, maxBytes, duration.TotalMinutes);
      return new SignedUpload(url, headers, clock.UtcNow().Add(duration));
    }
    catch (Exception ex) when (ex is not OperationCanceledException)
    {
      log.LogError(ex, "Ký URL upload thất bại cho {ObjectPath}", objectPath);   // KHÔNG log URL/chữ ký
      return Error.External("STORAGE_SIGN_FAILED", "Không tạo được URL upload.");
    }
  }

  public Result<SignedUrl> CreateDownloadUrl(string objectPath)
  {
    var o = options.Value;
    var duration = TimeSpan.FromMinutes(Math.Clamp(o.DownloadUrlMinutes, 1, 24 * 60));
    try
    {
      var template = UrlSigner.RequestTemplate.FromBucket(o.Bucket).WithObjectName(objectPath).WithHttpMethod(HttpMethod.Get);
      var url = _signer.Sign(template, UrlSigner.Options.FromDuration(duration).WithSigningVersion(SigningVersion.V4));
      return new SignedUrl(url, clock.UtcNow().Add(duration));
    }
    catch (Exception ex) when (ex is not OperationCanceledException)
    {
      log.LogError(ex, "Ký URL đọc thất bại cho {ObjectPath}", objectPath);
      return Error.External("STORAGE_SIGN_FAILED", "Không tạo được URL đọc.");
    }
  }

  public async Task<Result<StoredObject>> GetObjectAsync(string objectPath, CancellationToken ct)
  {
    try
    {
      var obj = await client.GetObjectAsync(options.Value.Bucket, objectPath, cancellationToken: ct);
      return new StoredObject((long)(obj.Size ?? 0), obj.ContentType);
    }
    catch (GoogleApiException ex) when (ex.HttpStatusCode == System.Net.HttpStatusCode.NotFound)
    {
      return Error.NotFound("MEDIA_NOT_UPLOADED", "File chưa được upload lên Storage.");
    }
    catch (GoogleApiException ex)
    {
      log.LogError(ex, "Đọc metadata {ObjectPath} thất bại (HTTP {Status})", objectPath, (int)ex.HttpStatusCode);
      return Error.External("STORAGE_UNAVAILABLE", "Không kiểm tra được file trên Storage.");
    }
  }
}
