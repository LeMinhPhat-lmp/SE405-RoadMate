using System.Threading.RateLimiting;

namespace RoadMate.Startup;

public static class WebApiExtensions
{
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
    });
    return services;
  }
}
