namespace RoadMate.Services.Notifications;

public enum NotificationMode
{
  /// <summary>Gửi thật tới mọi liên hệ.</summary>
  Live,
  /// <summary>Không gửi gì, chỉ ghi log và tạo delivery "dry-run" (cảnh báo giả lập mặc định).</summary>
  DryRun,
  /// <summary>Chỉ gửi thật tới số nằm trong Debug:AllowedPhones; số khác dry-run.</summary>
  AllowListOnly
}
