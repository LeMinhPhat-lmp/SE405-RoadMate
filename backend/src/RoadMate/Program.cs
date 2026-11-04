using Microsoft.Extensions.Hosting;
using RoadMate.Infrastructure.Diagnostics;
using RoadMate.Infrastructure.Logging;
using RoadMate.Startup;
using Serilog;

// Logger tạm để bắt lỗi xảy ra trước khi host dựng xong (thiếu cấu hình, lỗi DI…).
Log.Logger = new LoggerConfiguration().WriteTo.Console().CreateBootstrapLogger();

try
{
  var builder = WebApplication.CreateBuilder(args);
  var cfg = builder.Configuration;
  var useDevAuth = builder.Environment.IsDevelopment() && cfg.GetValue<bool>("Auth:UseDevAuth");

  builder.AddAppLogging();
  builder.Services
    .AddAppOptions(cfg)
    .AddDatabase(cfg)
    .AddAppAuthentication(cfg, useDevAuth)
    .AddSmsSender(cfg)
    .AddRepositories()
    .AddFeatureServices()
    .AddWebApi(cfg, builder.Environment)
    .AddAppSwagger(useDevAuth);
  var firebaseLoaded = builder.Services.AddFirebase(cfg);

  var app = builder.Build();

  ConfigDiagnostics.Report(cfg, app.Logger, firebaseLoaded, useDevAuth);
  app.EnsureDatabaseCreatedIfConfigured();

  // Thứ tự quan trọng: ForwardedHeaders (IP thật) -> CorrelationId (gán mã cho mọi log phía sau) -> request log
  // -> ExceptionHandler (bắt mọi exception, request log vẫn ghi được status 500) -> phần còn lại.
  if (cfg.GetValue<bool>("ForwardedHeaders:Enabled")) app.UseForwardedHeaders();
  app.UseMiddleware<CorrelationIdMiddleware>();
  app.UseAppRequestLogging();
  app.UseExceptionHandler();
  app.UseStatusCodePages();
  if (app.Environment.IsDevelopment()) { app.UseSwagger(); app.UseSwaggerUI(); }
  app.UseCors();
  app.UseRateLimiter();
  app.UseAuthentication();
  app.UseAuthorization();
  app.MapControllers();
  app.MapGet("/health", (TimeProvider clock) => Results.Ok(new { status = "ok", time = clock.GetUtcNow() })).AllowAnonymous();

  app.Run();
  return 0;
}
catch (Exception ex) when (ex is not HostAbortedException)   // HostAbortedException: công cụ EF (dotnet ef) dừng host có chủ đích
{
  Log.Fatal(ex, "RoadMate dừng bất thường");
  return 1;
}
finally
{
  Log.CloseAndFlush();
}
