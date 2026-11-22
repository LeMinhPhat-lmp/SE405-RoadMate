namespace RoadMate.Domain.Common;

/// <summary>Shared error catalogue (codes are API contract; messages are Vietnamese).</summary>
public static class Errors
{
  public const int MaxWaypoints = 20;
  public const int MaxContactsPerUser = 5;

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
  public static readonly Error InvalidReturnTime = Error.Validation("INVALID_RETURN_TIME", "Giờ dự kiến về phải ở tương lai.");
  public static readonly Error TooManyWaypoints = Error.Validation("TOO_MANY_WAYPOINTS", $"Tối đa {MaxWaypoints} điểm dừng.");
  public static readonly Error InvalidRest = Error.Validation("INVALID_REST", "Thời gian nghỉ chân từ 5 đến 480 phút.");
  public static readonly Error InvalidName = Error.Validation("INVALID_NAME", "Tên không được để trống.");
  public static readonly Error InvalidPhone = Error.Validation("INVALID_PHONE", "Số điện thoại Việt Nam không hợp lệ.");
  public static readonly Error ContactLimit = Error.Conflict("CONTACT_LIMIT", $"Tối đa {MaxContactsPerUser} liên hệ khẩn cấp.");
  public static readonly Error InvalidClientId = Error.Validation("INVALID_CLIENT_ID", "clientId bắt buộc (≤100 ký tự).");
  public static readonly Error InvalidStatus = Error.Validation("INVALID_STATUS", "Chỉ đóng được với trạng thái cancelled hoặc resolved.");
  public static readonly Error InvalidBattery = Error.Validation("INVALID_BATTERY", "Pin phải từ 0 đến 100.");
  public static readonly Error DeliveryInvalidState = Error.Conflict("DELIVERY_INVALID_STATE", "Lượt gửi thông báo không ở trạng thái phù hợp.");
}
