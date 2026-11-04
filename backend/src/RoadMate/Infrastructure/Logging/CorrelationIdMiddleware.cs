using System.Diagnostics;
using System.Text.RegularExpressions;
using Serilog.Context;

namespace RoadMate.Infrastructure.Logging;

/// <summary>
/// Gán mã tương quan cho mỗi request: dùng header X-Correlation-Id của client (nếu hợp lệ) hoặc TraceId của request.
/// Mã này có trong MỌI dòng log của request, trong response header và trong ProblemDetails -> tra log theo mã người dùng báo.
/// </summary>
public partial class CorrelationIdMiddleware(RequestDelegate next)
{
  [GeneratedRegex("^[A-Za-z0-9_.\\-]{1,64}$")]
  private static partial Regex SafeId();

  public async Task InvokeAsync(HttpContext ctx)
  {
    var id = ctx.Request.Headers[CorrelationKeys.HeaderName].FirstOrDefault();
    if (string.IsNullOrWhiteSpace(id) || !SafeId().IsMatch(id))      // chặn log injection / id quá dài
      id = Activity.Current?.TraceId.ToString() ?? Guid.NewGuid().ToString("N");

    ctx.Items[CorrelationKeys.ItemKey] = id;
    ctx.Response.OnStarting(() => { ctx.Response.Headers[CorrelationKeys.HeaderName] = id; return Task.CompletedTask; });
    using (LogContext.PushProperty("CorrelationId", id)) await next(ctx);
  }
}
