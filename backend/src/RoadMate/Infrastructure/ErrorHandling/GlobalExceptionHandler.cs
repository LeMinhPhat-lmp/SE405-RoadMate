using System.Text.Json;
using Microsoft.AspNetCore.Diagnostics;
using RoadMate.Data;

namespace RoadMate.Infrastructure.ErrorHandling;

/// <summary>
/// Chốt chặn cuối cho exception KHÔNG lường trước (lỗi nghiệp vụ đã đi qua Result pattern, không vào đây).
/// Trả ProblemDetails thống nhất, KHÔNG lộ stack trace/chi tiết nội bộ ra client (trừ môi trường Development),
/// và ghi log đầy đủ (exception + traceId + correlationId nằm sẵn trong LogContext).
/// </summary>
public sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> log, IHostEnvironment env) : IExceptionHandler
{
  /// <summary>Nginx dùng 499 cho "client đóng kết nối trước khi server trả lời".</summary>
  private const int ClientClosedRequest = 499;

  public async ValueTask<bool> TryHandleAsync(HttpContext http, Exception ex, CancellationToken ct)
  {
    if (ex is OperationCanceledException && http.RequestAborted.IsCancellationRequested)
    {
      log.LogInformation("Client huỷ request {Method} {Path}", http.Request.Method, http.Request.Path);
      http.Response.StatusCode = ClientClosedRequest;
      return true;
    }

    var error = Map(ex);
    var status = error.Type.ToStatusCode();
    if (status >= 500) log.LogError(ex, "Lỗi không xử lý được ở {Method} {Path} -> {Status} {Code}", http.Request.Method, http.Request.Path, status, error.Code);
    else log.LogWarning(ex, "Request bị từ chối do exception ở {Method} {Path} -> {Status} {Code}", http.Request.Method, http.Request.Path, status, error.Code);

    var pd = ProblemFactory.Create(http, status, error.Code, error.Message);
    if (env.IsDevelopment()) pd.Extensions["exception"] = $"{ex.GetType().Name}: {ex.Message}";

    http.Response.StatusCode = status;
    await http.Response.WriteAsJsonAsync(pd, options: null, contentType: "application/problem+json", cancellationToken: ct);
    return true;
  }

  private static Error Map(Exception ex) => ex switch
  {
    UnauthorizedAccessException => Error.Unauthorized("UNAUTHORIZED", "Bạn chưa đăng nhập hoặc phiên đăng nhập không hợp lệ."),
    BadHttpRequestException or JsonException => Error.Validation("BAD_REQUEST", "Dữ liệu gửi lên không đúng định dạng."),
    DbUpdateConcurrencyException => Error.Conflict("CONCURRENCY_CONFLICT", "Dữ liệu vừa được thay đổi ở nơi khác, hãy tải lại rồi thử lại."),
    DbUpdateException db when db.IsUniqueViolation() => Error.Conflict("DUPLICATE_RECORD", "Dữ liệu đã tồn tại."),
    DbUpdateException db when db.IsForeignKeyViolation() => Error.Conflict("RELATED_RECORD_MISSING", "Dữ liệu liên quan không còn tồn tại."),
    HttpRequestException => Error.External("UPSTREAM_ERROR", "Dịch vụ bên ngoài đang gặp sự cố, hãy thử lại sau."),
    _ => Error.Failure("INTERNAL_ERROR", "Đã xảy ra lỗi hệ thống. Vui lòng thử lại hoặc cung cấp mã traceId cho đội phát triển.")
  };
}
