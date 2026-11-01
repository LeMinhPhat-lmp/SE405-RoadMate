using RoadMate.Infrastructure.Diagnostics;
using RoadMate.Startup;

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
