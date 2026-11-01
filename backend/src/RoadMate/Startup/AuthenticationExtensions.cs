using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace RoadMate.Startup;

public static class AuthenticationExtensions
{
  /// <summary>Firebase ID token (hoặc DevAuth khi phát triển) + ngữ cảnh người dùng hiện tại (Firebase UID -> Id nội bộ).</summary>
  public static IServiceCollection AddAppAuthentication(this IServiceCollection services, IConfiguration cfg, bool useDevAuth)
  {
    if (useDevAuth)
    {
      services.AddAuthentication(DevAuthHandler.Scheme)
        .AddScheme<AuthenticationSchemeOptions, DevAuthHandler>(DevAuthHandler.Scheme, _ => { });
    }
    else
    {
      var projectId = cfg["Firebase:ProjectId"] ?? throw new InvalidOperationException("Thiếu Firebase:ProjectId");
      services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(o =>
      {
        o.Authority = $"https://securetoken.google.com/{projectId}";
        o.MapInboundClaims = false;
        o.TokenValidationParameters = new TokenValidationParameters
        {
          ValidateIssuer = true,
          ValidIssuer = $"https://securetoken.google.com/{projectId}",
          ValidateAudience = true,
          ValidAudience = projectId,
          ValidateLifetime = true
        };
      });
    }
    services.AddAuthorization();
    services.AddMemoryCache();
    services.AddHttpContextAccessor();
    services.AddScoped<ICurrentUserContext, HttpCurrentUserContext>();
    services.AddScoped<IClaimsTransformation, UserContextClaimsTransformation>();
    return services;
  }
}
