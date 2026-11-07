namespace RoadMate.Common;

/// <summary>Danh mục lỗi dùng chung giữa nhiều service. Lỗi chỉ dùng ở một nơi thì tạo trực tiếp bằng Error.Xxx.</summary>
public static class Errors
{
  public static readonly Error UserNotSynced = Error.Conflict("USER_NOT_SYNCED", "Chưa đồng bộ tài khoản, hãy gọi POST /auth/sync sau khi đăng nhập.");
  public static readonly Error TripNotFound = Error.NotFound("TRIP_NOT_FOUND", "Không tìm thấy chuyến đi.");
  public static readonly Error TripNotActive = Error.Conflict("TRIP_NOT_ACTIVE", "Chuyến đi chưa bắt đầu hoặc đã kết thúc.");
  public static readonly Error TripNotDraft = Error.Conflict("TRIP_NOT_DRAFT", "Chỉ thao tác được với chuyến ở trạng thái nháp.");
  public static readonly Error TripFinished = Error.Conflict("TRIP_FINISHED", "Chuyến đã kết thúc.");
  public static readonly Error AlertNotFound = Error.NotFound("ALERT_NOT_FOUND", "Không tìm thấy cảnh báo.");
  public static readonly Error AlertClosed = Error.Conflict("ALERT_CLOSED", "Cảnh báo đã đóng.");
  public static readonly Error ContactNotFound = Error.NotFound("CONTACT_NOT_FOUND", "Không tìm thấy liên hệ.");
  public static readonly Error MemoryNotFound = Error.NotFound("MEMORY_NOT_FOUND", "Không tìm thấy kỷ niệm.");
  public static readonly Error LinkInvalid = Error.NotFound("LINK_INVALID", "Link theo dõi không tồn tại hoặc đã hết hạn.");
  public static readonly Error AckInvalid = Error.NotFound("ACK_LINK_INVALID", "Link xác nhận không tồn tại.");
  public static readonly Error InvalidLocation = Error.Validation("INVALID_LOCATION", "Toạ độ không hợp lệ.");
  public static readonly Error InvalidSensitivity = Error.Validation("INVALID_SENSITIVITY", "crashSensitivity phải là low|medium|high.");
  public static readonly Error InvalidIdle = Error.Validation("INVALID_IDLE", "idleMinutes phải từ 1 đến 240.");
}
