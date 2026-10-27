using System.Threading.RateLimiting;
using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using RoadMate.Data;
using RoadMate.Infrastructure;
using RoadMate.Infrastructure.Implements;
using RoadMate.Repositories.Implements;
using RoadMate.Services.Implements;

var builder = WebApplication.CreateBuilder(args);
var cfg = builder.Configuration;

// ---------- Database (PostgreSQL)
builder.Services.AddDbContext<AppDbContext>(o => o.UseNpgsql(cfg.GetConnectionString("Default")));

// ---------- Options
builder.Services.Configure<AlertOptions>(cfg.GetSection("Alert"));
builder.Services.Configure<TrackingOptions>(cfg.GetSection("Tracking"));
builder.Services.Configure<GoongOptions>(cfg.GetSection("Goong"));
builder.Services.Configure<SmsOptions>(cfg.GetSection("Sms"));

// ---------- Auth: Firebase ID token (hoặc DevAuth khi phát triển)
var useDevAuth = builder.Environment.IsDevelopment() && cfg.GetValue<bool>("Auth:UseDevAuth");
if (useDevAuth)
{
  builder.Services.AddAuthentication(DevAuthHandler.Scheme)
    .AddScheme<AuthenticationSchemeOptions, DevAuthHandler>(DevAuthHandler.Scheme, _ => { });
}
else
{
  var projectId = cfg["Firebase:ProjectId"] ?? throw new InvalidOperationException("Thiếu Firebase:ProjectId");
  builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(o =>
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
builder.Services.AddAuthorization();
builder.Services.AddMemoryCache();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserContext, HttpCurrentUserContext>();
builder.Services.AddScoped<IClaimsTransformation, UserContextClaimsTransformation>();   // Firebase UID -> Id nội bộ (Guid)

// ---------- Firebase Admin (FCM). Không có credential thì dùng LogPushSender.
var credPath = cfg["Firebase:CredentialPath"];
var firebaseLoaded = !string.IsNullOrWhiteSpace(credPath) && File.Exists(credPath);
if (firebaseLoaded)
{
  FirebaseApp.Create(new AppOptions { Credential = GoogleCredential.FromFile(credPath) });
  builder.Services.AddSingleton<IPushSender, FcmPushSender>();
}
else builder.Services.AddSingleton<IPushSender, LogPushSender>();

// ---------- SMS
if (string.Equals(cfg["Sms:Provider"], "esms", StringComparison.OrdinalIgnoreCase)) builder.Services.AddHttpClient<ISmsSender, ESmsSender>();
else builder.Services.AddSingleton<ISmsSender, LogSmsSender>();

// ---------- Repositories + UnitOfWork
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IEmergencyContactRepository, EmergencyContactRepository>();
builder.Services.AddScoped<ITripRepository, TripRepository>();
builder.Services.AddScoped<ITripPointRepository, TripPointRepository>();
builder.Services.AddScoped<IMemoryRepository, MemoryRepository>();
builder.Services.AddScoped<IAlertRepository, AlertRepository>();
builder.Services.AddScoped<IAlertDeliveryRepository, AlertDeliveryRepository>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// ---------- Services
builder.Services.AddScoped<IAlertDispatcher, AlertDispatcher>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IEmergencyContactService, EmergencyContactService>();
builder.Services.AddScoped<ITripService, TripService>();
builder.Services.AddScoped<ITrackingService, TrackingService>();
builder.Services.AddScoped<IJournalService, JournalService>();
builder.Services.AddScoped<IAlertService, AlertService>();
builder.Services.AddHttpClient<IMapService, MapService>();
builder.Services.AddHostedService<AlertRepeatWorker>();

// ---------- Web
builder.Services.AddControllers(o => o.Filters.Add<SyncedUserFilter>());
builder.Services.AddProblemDetails();
builder.Services.AddCors(o => o.AddDefaultPolicy(p => p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));   // TODO production: giới hạn origin
builder.Services.AddRateLimiter(o =>
{
  o.RejectionStatusCode = 429;
  o.AddPolicy("public", ctx => RateLimitPartition.GetFixedWindowLimiter(
    ctx.Connection.RemoteIpAddress?.ToString() ?? "anon",
    _ => new FixedWindowRateLimiterOptions { PermitLimit = 60, Window = TimeSpan.FromMinutes(1) }));
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
  c.SwaggerDoc("v1", new OpenApiInfo { Title = "RoadMate API", Version = "v1" });
  var scheme = useDevAuth
    ? new OpenApiSecurityScheme { Name = "X-Dev-Uid", In = ParameterLocation.Header, Type = SecuritySchemeType.ApiKey, Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "DevUid" } }
    : new OpenApiSecurityScheme { Name = "Authorization", In = ParameterLocation.Header, Type = SecuritySchemeType.Http, Scheme = "bearer", BearerFormat = "Firebase ID token", Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } };
  c.AddSecurityDefinition(scheme.Reference.Id, scheme);
  c.AddSecurityRequirement(new OpenApiSecurityRequirement { [scheme] = Array.Empty<string>() });
});

var app = builder.Build();

ConfigDiagnostics.Report(cfg, app.Logger, firebaseLoaded, useDevAuth);

if (cfg.GetValue<bool>("Database:EnsureCreated"))
{
  using var scope = app.Services.CreateScope();
  scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.EnsureCreated();   // dev nhanh; sau này chuyển sang EF migrations
}

app.UseExceptionHandler();
if (app.Environment.IsDevelopment()) { app.UseSwagger(); app.UseSwaggerUI(); }
app.UseCors();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapGet("/health", () => Results.Ok(new { status = "ok", time = DateTime.UtcNow })).AllowAnonymous();
app.Run();
