using RoadMate.Infrastructure.Diagnostics;
using RoadMate.Startup;

// Nạp .env (GOONG_API_KEY…) vào biến môi trường trước khi tạo host; biến môi trường thật luôn được ưu tiên.
var dotEnvPath = DotEnv.Load();

var builder = WebApplication.CreateBuilder(args);
var cfg = builder.Configuration;
var useDevAuth = builder.Environment.IsDevelopment() && cfg.GetValue<bool>("Auth:UseDevAuth");

builder.Services
  .AddAppOptions(cfg)
  .AddDatabase(cfg)
  .AddAppAuthentication(cfg, useDevAuth)
  .AddSmsSender(cfg)
  .AddRepositories()
  .AddFeatureServices()
  .AddWebApi()
  .AddAppSwagger(useDevAuth);
var firebaseLoaded = builder.Services.AddFirebasePush(cfg);

var app = builder.Build();

if (dotEnvPath is not null) app.Logger.LogInformation("[CONFIG] Đã nạp biến môi trường từ {Path}", dotEnvPath);
ConfigDiagnostics.Report(cfg, app.Logger, firebaseLoaded, useDevAuth);
app.EnsureDatabaseCreatedIfConfigured();

app.UseExceptionHandler();
if (app.Environment.IsDevelopment()) { app.UseSwagger(); app.UseSwaggerUI(); }
app.UseCors();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapGet("/health", () => Results.Ok(new { status = "ok", time = DateTime.UtcNow })).AllowAnonymous();
app.Run();
