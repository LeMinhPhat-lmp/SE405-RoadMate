using System.Security.Cryptography;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.HttpOverrides;
using RoadMate.Infrastructure.ErrorHandling;

namespace RoadMate.Startup;

public static class WebApiExtensions
{
  /// <summary>Số request/phút/người dùng cho nhóm routes/* và places/* (bảo vệ hạn mức Goong).</summary>
  public const int MapsPermitPerMinute = 120;
  public const int PublicPermitPerMinute = 60;

  public static IServiceCollection AddWebApi(this IServiceCollection services, IConfiguration cfg, IHostEnvironment env)
  {
    services.AddControllers(o => o.Filters.Add<SyncedUserFilter>());
    services.AddExceptionHandler<GlobalExceptionHandler>();
    services.AddProblemDetails(o => o.CustomizeProblemDetails = ctx =>
    {
      // Áp dụng cho ProblemDetails do framework tự sinh (404 không có route, 401/403, 415…).
      var http = ctx.HttpContext;
      ctx.ProblemDetails.Extensions["traceId"] = System.Diagnostics.Activity.Current?.TraceId.ToString() ?? http.TraceIdentifier;
      if (http.Items.TryGetValue(CorrelationKeys.ItemKey, out var id)) ctx.ProblemDetails.Extensions["correlationId"] = id;
    });
    services.AddAppCors(cfg, env);
    services.AddAppRateLimiter();
    services.AddForwardedHeadersIfEnabled(cfg);
    return services;
  }

  /// <summary>CORS: chỉ các origin trong Cors:AllowedOrigins (vd trang theo dõi của người thân). Dev mà chưa khai báo thì mở hết.</summary>
  private static void AddAppCors(this IServiceCollection services, IConfiguration cfg, IHostEnvironment env)
  {
    var origins = cfg.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
    services.AddCors(o => o.AddDefaultPolicy(p =>
    {
      if (origins.Length > 0) p.WithOrigins(origins);
      else if (env.IsDevelopment()) p.AllowAnyOrigin();
      p.AllowAnyHeader().AllowAnyMethod().WithExposedHeaders(CorrelationKeys.HeaderName);
    }));
  }

  private static void AddAppRateLimiter(this IServiceCollection services) => services.AddRateLimiter(o =>
  {
    o.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    o.OnRejected = async (ctx, ct) =>
    {
      var http = ctx.HttpContext;
      if (ctx.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter)) http.Response.Headers.RetryAfter = ((int)retryAfter.TotalSeconds).ToString();
      var pd = ProblemFactory.Create(http, StatusCodes.Status429TooManyRequests, "TOO_MANY_REQUESTS", "Bạn gửi quá nhiều yêu cầu, hãy thử lại sau.");
      http.Response.StatusCode = StatusCodes.Status429TooManyRequests;
      await http.Response.WriteAsJsonAsync(pd, options: null, contentType: "application/problem+json", cancellationToken: ct);
    };
    o.AddPolicy("public", ctx => RateLimitPartition.GetFixedWindowLimiter(
      ctx.Connection.RemoteIpAddress?.ToString() ?? "anon",
      _ => new FixedWindowRateLimiterOptions { PermitLimit = PublicPermitPerMinute, Window = TimeSpan.FromMinutes(1) }));
    // UseRateLimiter chạy TRƯỚC UseAuthentication nên chưa có ctx.User: phân vùng theo hash của token (hoặc X-Dev-Uid khi dev), thiếu thì theo IP.
    o.AddPolicy("maps", ctx => RateLimitPartition.GetFixedWindowLimiter(
      MapsPartitionKey(ctx),
      _ => new FixedWindowRateLimiterOptions { PermitLimit = MapsPermitPerMinute, Window = TimeSpan.FromMinutes(1) }));
  });

  /// <summary>
  /// ForwardedHeaders:Enabled=true khi chạy sau reverse proxy (Caddy/Nginx) để RemoteIpAddress là IP thật của client
  /// (rate limit theo IP và log đúng). Chỉ bật khi API KHÔNG mở thẳng ra Internet, vì sẽ tin header X-Forwarded-* từ mọi nguồn.
  /// </summary>
  private static void AddForwardedHeadersIfEnabled(this IServiceCollection services, IConfiguration cfg)
  {
    if (!cfg.GetValue<bool>("ForwardedHeaders:Enabled")) return;
    services.Configure<ForwardedHeadersOptions>(o =>
    {
      o.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
      o.KnownIPNetworks.Clear();
      o.KnownProxies.Clear();
    });
  }

  private static string MapsPartitionKey(HttpContext ctx)
  {
    var raw = ctx.Request.Headers.Authorization.ToString();
    if (string.IsNullOrEmpty(raw)) raw = ctx.Request.Headers["X-Dev-Uid"].ToString();
    if (string.IsNullOrEmpty(raw)) return "ip:" + (ctx.Connection.RemoteIpAddress?.ToString() ?? "anon");
    return "u:" + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(raw)))[..16];
  }
}
