using System.Security.Cryptography;
using System.Threading.RateLimiting;

namespace RoadMate.Startup;

public static class WebApiExtensions
{
  /// <summary>Số request/phút/người dùng cho nhóm routes/* và places/* (bảo vệ hạn mức Goong).</summary>
  public const int MapsPermitPerMinute = 120;

  public static IServiceCollection AddWebApi(this IServiceCollection services)
  {
    services.AddControllers(o => o.Filters.Add<SyncedUserFilter>());
    services.AddProblemDetails();
    services.AddCors(o => o.AddDefaultPolicy(p => p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));   // TODO production: giới hạn origin
    services.AddRateLimiter(o =>
    {
      o.RejectionStatusCode = 429;
      o.AddPolicy("public", ctx => RateLimitPartition.GetFixedWindowLimiter(
        ctx.Connection.RemoteIpAddress?.ToString() ?? "anon",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 60, Window = TimeSpan.FromMinutes(1) }));
      // UseRateLimiter chạy TRƯỚC UseAuthentication nên chưa có ctx.User: phân vùng theo hash của token (hoặc X-Dev-Uid khi dev), thiếu thì theo IP.
      o.AddPolicy("maps", ctx => RateLimitPartition.GetFixedWindowLimiter(
        MapsPartitionKey(ctx),
        _ => new FixedWindowRateLimiterOptions { PermitLimit = MapsPermitPerMinute, Window = TimeSpan.FromMinutes(1) }));
    });
    return services;
  }

  private static string MapsPartitionKey(HttpContext ctx)
  {
    var raw = ctx.Request.Headers.Authorization.ToString();
    if (string.IsNullOrEmpty(raw)) raw = ctx.Request.Headers["X-Dev-Uid"].ToString();
    if (string.IsNullOrEmpty(raw)) return "ip:" + (ctx.Connection.RemoteIpAddress?.ToString() ?? "anon");
    return "u:" + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(raw)))[..16];
  }
}
