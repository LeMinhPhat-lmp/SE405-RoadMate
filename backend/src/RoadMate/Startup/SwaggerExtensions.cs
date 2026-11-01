using Microsoft.OpenApi.Models;

namespace RoadMate.Startup;

public static class SwaggerExtensions
{
  public static IServiceCollection AddAppSwagger(this IServiceCollection services, bool useDevAuth)
  {
    services.AddEndpointsApiExplorer();
    services.AddSwaggerGen(c =>
    {
      c.SwaggerDoc("v1", new OpenApiInfo { Title = "RoadMate API", Version = "v1" });
      var scheme = useDevAuth
        ? new OpenApiSecurityScheme { Name = "X-Dev-Uid", In = ParameterLocation.Header, Type = SecuritySchemeType.ApiKey, Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "DevUid" } }
        : new OpenApiSecurityScheme { Name = "Authorization", In = ParameterLocation.Header, Type = SecuritySchemeType.Http, Scheme = "bearer", BearerFormat = "Firebase ID token", Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } };
      c.AddSecurityDefinition(scheme.Reference.Id, scheme);
      c.AddSecurityRequirement(new OpenApiSecurityRequirement { [scheme] = Array.Empty<string>() });
    });
    return services;
  }
}
