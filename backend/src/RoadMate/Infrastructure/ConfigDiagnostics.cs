namespace RoadMate.Infrastructure;

/// <summary>Ghi cảnh báo lúc khởi động cho những tính năng đang thiếu cấu hình thật (xem docs/SETUP_AND_LIMITATIONS.md).</summary>
public static class ConfigDiagnostics
{
  public static void Report(IConfiguration cfg, ILogger log, bool firebaseCredentialLoaded, bool devAuth)
  {
    if (devAuth) log.LogWarning("[CONFIG] DevAuth đang bật: mọi request chỉ cần header X-Dev-Uid. Tuyệt đối không dùng ở production.");
    if (!devAuth && (cfg["Firebase:ProjectId"] ?? "").StartsWith("YOUR_"))
      log.LogWarning("[CONFIG] Firebase:ProjectId vẫn là giá trị mẫu: không xác thực được token thật.");
    if (string.IsNullOrWhiteSpace(cfg["Goong:ApiKey"]))
      log.LogWarning("[CONFIG] Thiếu Goong:ApiKey: /routes/directions và /places/* sẽ trả lỗi MAP_NOT_CONFIGURED.");
    if (!string.Equals(cfg["Sms:Provider"], "esms", StringComparison.OrdinalIgnoreCase))
      log.LogWarning("[CONFIG] Sms:Provider={Provider}: SMS cảnh báo CHỈ ĐƯỢC GHI LOG, người thân sẽ không nhận được tin nhắn thật.", cfg["Sms:Provider"] ?? "log");
    else if (string.IsNullOrWhiteSpace(cfg["Sms:ESms:ApiKey"]) || string.IsNullOrWhiteSpace(cfg["Sms:ESms:SecretKey"]))
      log.LogWarning("[CONFIG] Sms:Provider=esms nhưng thiếu ApiKey/SecretKey.");
    if (!firebaseCredentialLoaded)
      log.LogWarning("[CONFIG] Chưa nạp Firebase:CredentialPath: push FCM CHỈ ĐƯỢC GHI LOG.");
    if ((cfg["Tracking:BaseUrl"] ?? "").Contains("roadmate.app"))
      log.LogWarning("[CONFIG] Tracking:BaseUrl đang là domain mẫu (roadmate.app): link theo dõi gửi cho người thân chưa mở được.");
    if (cfg.GetValue<bool>("Debug:EnableSimulator")) log.LogWarning("[CONFIG] Debug:EnableSimulator đang bật.");
  }
}
