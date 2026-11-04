using RoadMate.Infrastructure.Logging;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.Elasticsearch;

namespace RoadMate.Startup;

public static class LoggingExtensions
{
  /// <summary>
  /// Serilog thay logging mặc định. Mức log + override: section "Serilog:MinimumLevel". Nơi ghi: section "Observability"
  /// (console, file, Elasticsearch -> Kibana). Mỗi dòng log có Application, Environment, Host, CorrelationId.
  /// </summary>
  public static WebApplicationBuilder AddAppLogging(this WebApplicationBuilder builder)
  {
    builder.Host.UseSerilog((ctx, services, lc) =>
    {
      var o = ctx.Configuration.GetSection(ObservabilityOptions.Section).Get<ObservabilityOptions>() ?? new ObservabilityOptions();
      lc.ReadFrom.Configuration(ctx.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .Enrich.WithProperty("Application", "RoadMate.Api")
        .Enrich.WithProperty("Environment", ctx.HostingEnvironment.EnvironmentName)
        .Enrich.WithProperty("Host", Environment.MachineName);

      if (o.ConsoleEnabled)
        lc.WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {CorrelationId} {SourceContext}: {Message:lj}{NewLine}{Exception}");
      if (o.File.Enabled)
        lc.WriteTo.File(o.File.Path, rollingInterval: RollingInterval.Day, retainedFileCountLimit: o.File.RetainedFiles,
          outputTemplate: "{Timestamp:o} [{Level:u3}] {CorrelationId} {SourceContext}: {Message:lj}{NewLine}{Exception}");
      if (o.Elasticsearch.Enabled) lc.WriteTo.Elasticsearch(BuildElasticSink(o.Elasticsearch));
    });
    return builder;
  }

  private static ElasticsearchSinkOptions BuildElasticSink(ElasticLogOptions es)
  {
    var options = new ElasticsearchSinkOptions(new Uri(es.Uri))
    {
      IndexFormat = es.IndexFormat,
      AutoRegisterTemplate = true,
      AutoRegisterTemplateVersion = AutoRegisterTemplateVersion.ESv7,
      TypeName = null,                                         // Elasticsearch 8 không còn mapping type
      NumberOfShards = 1,
      NumberOfReplicas = 0,
      BufferBaseFilename = es.BufferPath,                      // đệm đĩa: ES chết tạm thời thì không mất log
      EmitEventFailure = EmitEventFailureHandling.WriteToSelfLog
    };
    if (!string.IsNullOrWhiteSpace(es.Username))
      options.ModifyConnectionSettings = c => c.BasicAuthentication(es.Username, es.Password ?? "");
    return options;
  }

  /// <summary>Một dòng log mỗi request (method, path, status, thời gian, UserId, ClientIp). /health chỉ ở mức Verbose để không làm nhiễu.</summary>
  public static WebApplication UseAppRequestLogging(this WebApplication app)
  {
    app.UseSerilogRequestLogging(o =>
    {
      o.MessageTemplate = "HTTP {RequestMethod} {RequestPath} -> {StatusCode} trong {Elapsed:0.0} ms";
      o.GetLevel = (http, _, ex) =>
        http.Request.Path.StartsWithSegments("/health") ? LogEventLevel.Verbose
        : ex is not null || http.Response.StatusCode >= 500 ? LogEventLevel.Error
        : http.Response.StatusCode >= 400 ? LogEventLevel.Warning
        : LogEventLevel.Information;
      o.EnrichDiagnosticContext = (diagnostics, http) =>
      {
        diagnostics.Set("UserId", http.User.FindFirstValue(UserClaims.InternalId));
        diagnostics.Set("ClientIp", http.Connection.RemoteIpAddress?.ToString());
        diagnostics.Set("UserAgent", http.Request.Headers.UserAgent.ToString());
      };
    });
    return app;
  }
}
