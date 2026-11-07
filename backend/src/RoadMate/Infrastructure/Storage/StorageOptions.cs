namespace RoadMate.Infrastructure.Storage;

/// <summary>Firebase Storage (GCS) – section "Storage". Credential dùng chung với Firebase:CredentialPath (service account).</summary>
public class StorageOptions
{
  /// <summary>Tên bucket (phần sau gs://), vd se405-roadmate.firebasestorage.app.</summary>
  public string Bucket { get; set; } = "";
  public int UploadUrlMinutes { get; set; } = 15;
  public int DownloadUrlMinutes { get; set; } = 60;
  public long MaxImageBytes { get; set; } = 5 * 1024 * 1024;
  public long MaxVideoBytes { get; set; } = 25 * 1024 * 1024;
  /// <summary>true: khi tạo kỷ niệm, server kiểm tra object đã thực sự được upload (tồn tại, đúng loại, không vượt dung lượng).</summary>
  public bool VerifyOnCreate { get; set; } = true;
  /// <summary>true: ký kèm header x-goog-content-length-range để GCS tự từ chối file vượt dung lượng khai báo.</summary>
  public bool EnforceSizeLimit { get; set; } = true;
}
