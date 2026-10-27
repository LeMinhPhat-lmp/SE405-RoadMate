namespace RoadMate.Common;

public static class ResultExtensions
{
  public static ObjectResult ToProblem(this Error e)
  {
    var status = e.Type switch
    {
      ErrorType.Validation => 400,
      ErrorType.Unauthorized => 401,
      ErrorType.Forbidden => 403,
      ErrorType.NotFound => 404,
      ErrorType.Conflict => 409,
      ErrorType.TooManyRequests => 429,
      ErrorType.External => 502,
      ErrorType.NotConfigured => 503,
      _ => 500
    };
    var pd = new ProblemDetails { Status = status, Title = e.Code, Detail = e.Message };
    return new ObjectResult(pd) { StatusCode = status };
  }
}

public static class Errors
{
  public static readonly Error UserNotSynced = Error.Conflict("USER_NOT_SYNCED", "Chưa đồng bộ tài khoản, hãy gọi POST /auth/sync sau khi đăng nhập.");
  public static readonly Error TripNotFound = Error.NotFound("TRIP_NOT_FOUND", "Không tìm thấy chuyến đi.");
  public static readonly Error AlertNotFound = Error.NotFound("ALERT_NOT_FOUND", "Không tìm thấy cảnh báo.");
  public static readonly Error ContactNotFound = Error.NotFound("CONTACT_NOT_FOUND", "Không tìm thấy liên hệ.");
  public static readonly Error MemoryNotFound = Error.NotFound("MEMORY_NOT_FOUND", "Không tìm thấy kỷ niệm.");
  public static readonly Error TripNotActive = Error.Conflict("TRIP_NOT_ACTIVE", "Chuyến đi chưa bắt đầu hoặc đã kết thúc.");
}
