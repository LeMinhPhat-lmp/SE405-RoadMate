using System.Diagnostics;

namespace RoadMate.Common;

/// <summary>Nơi DUY NHẤT dựng ProblemDetails: mọi lỗi trả về client (nghiệp vụ, exception, rate limit) có chung hình dạng.</summary>
public static class ProblemFactory
{
  public static ProblemDetails Create(HttpContext http, int status, string code, string detail)
  {
    var pd = new ProblemDetails { Status = status, Title = code, Detail = detail };
    pd.Extensions["code"] = code;
    pd.Extensions["traceId"] = Activity.Current?.TraceId.ToString() ?? http.TraceIdentifier;
    if (http.Items.TryGetValue(CorrelationKeys.ItemKey, out var correlationId)) pd.Extensions["correlationId"] = correlationId;
    return pd;
  }
}
