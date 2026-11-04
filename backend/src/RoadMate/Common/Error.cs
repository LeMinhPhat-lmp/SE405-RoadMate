namespace RoadMate.Common;

public enum ErrorType { Failure, Validation, Unauthorized, Forbidden, NotFound, Conflict, TooManyRequests, External, NotConfigured }

public sealed record Error(string Code, string Message, ErrorType Type = ErrorType.Failure)
{
  public static readonly Error None = new(string.Empty, string.Empty);
  public static Error Failure(string code, string msg) => new(code, msg, ErrorType.Failure);
  public static Error Validation(string code, string msg) => new(code, msg, ErrorType.Validation);
  public static Error Unauthorized(string code, string msg) => new(code, msg, ErrorType.Unauthorized);
  public static Error NotFound(string code, string msg) => new(code, msg, ErrorType.NotFound);
  public static Error Conflict(string code, string msg) => new(code, msg, ErrorType.Conflict);
  public static Error Forbidden(string code, string msg) => new(code, msg, ErrorType.Forbidden);
  public static Error TooManyRequests(string code, string msg) => new(code, msg, ErrorType.TooManyRequests);
  public static Error External(string code, string msg) => new(code, msg, ErrorType.External);
  public static Error NotConfigured(string code, string msg) => new(code, msg, ErrorType.NotConfigured);   // thiếu cấu hình -> HTTP 503
}
