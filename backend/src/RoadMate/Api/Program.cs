using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using RoadMate.Infrastructure.Firebase;
using RoadMate.Api.Diagnostics;
using RoadMate.Api.Logging;
using RoadMate.Api.Startup;
using RoadMate.Application;
using RoadMate.Infrastructure;
using Serilog;
using DotNetEnv;

// Logger tạm để bắt lỗi xảy ra trước khi host dựng xong (thiếu cấu hình, lỗi DI…).
Log.Logger = new LoggerConfiguration().WriteTo.Console().CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);
    var environment = builder.Environment.EnvironmentName;
    var cfg = builder.Configuration;
    var useDevAuth = builder.Environment.IsDevelopment() && cfg.GetValue<bool>("Auth:UseDevAuth");

    var envFile = $".env.{environment}";
    if (File.Exists(envFile))
        Env.Load(envFile);
    builder.Configuration.AddEnvironmentVariables();

    builder.AddAppLogging();
    builder.Services
      .AddAppOptions(cfg)
      .AddSingleton(TimeProvider.System)
      .AddAppAuthentication(cfg, useDevAuth)
      .AddApplication()                 // Domain policies, validators, services, event handlers
      .AddInfrastructure(cfg)           // EF/Postgres, repositories, outbox channels, Goong, Open-Meteo, Firebase
      .AddAppWorkers()
      .AddWebApi(cfg, builder.Environment)
      .AddAppSwagger(useDevAuth);

    var app = builder.Build();

    ConfigDiagnostics.Report(cfg, app.Logger, app.Services.GetRequiredService<FirebaseContext>().IsConfigured, useDevAuth);
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
    // live = process is up (no dependencies); ready = database reachable. /health stays as a backward-compatible alias of live.
    app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false }).AllowAnonymous();
    app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = r => r.Tags.Contains("ready") }).AllowAnonymous();
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

/// <summary>Marker so WebApplicationFactory&lt;Program&gt; can find the entry point (top-level statements).</summary>
public partial class Program;
