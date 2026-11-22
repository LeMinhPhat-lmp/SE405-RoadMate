using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Testcontainers.PostgreSql;
using System.Linq;
using Microsoft.AspNetCore.Hosting;

namespace RoadMate.Tests.Api;

/// <summary>
/// Real PostgreSQL 16 (Testcontainers) + the real app (WebApplicationFactory) with DevAuth, log SMS/push providers and the simulator on.
/// Needs Docker. NOT executed in the authoring environment (no NuGet, no Docker): treat as unverified until the first CI run.
/// </summary>
public sealed class ApiFixture : IAsyncLifetime
{
  private readonly PostgreSqlContainer _pg = new PostgreSqlBuilder().WithImage("postgres:16").Build();
  public WebApplicationFactory<Program> Factory { get; private set; } = default!;

  public async Task InitializeAsync()
  {
    await _pg.StartAsync();
    Factory = new WebApplicationFactory<Program>().WithWebHostBuilder(b =>
    {
      b.UseEnvironment("Development");
      b.ConfigureAppConfiguration((_, c) => c.AddInMemoryCollection(new Dictionary<string, string?>
      {
        ["ConnectionStrings:Default"] = _pg.GetConnectionString(),
        ["Database:EnsureCreated"] = "true",
        ["Auth:UseDevAuth"] = "true",
        ["Debug:EnableSimulator"] = "true",
        ["Sms:Provider"] = "log",
        ["Firebase:CredentialPath"] = "",
        ["Observability:Elasticsearch:Enabled"] = "false",
        ["Alert:MaxRepeats"] = "3"
      }));
      // Background workers are driven explicitly by the tests (deterministic), not by timers.
      b.ConfigureServices(s => { foreach (var d in s.Where(x => x.ServiceType == typeof(IHostedService)).ToList()) s.Remove(d); });
    });
  }

  public HttpClient ClientFor(string uid, string? phone = null)
  {
    var c = Factory.CreateClient();
    c.DefaultRequestHeaders.Add("X-Dev-Uid", uid);
    if (phone is not null) c.DefaultRequestHeaders.Add("X-Dev-Phone", phone);
    return c;
  }

  public IServiceScope Scope() => Factory.Services.CreateScope();

  public async Task DisposeAsync() { await Factory.DisposeAsync(); await _pg.DisposeAsync(); }
}

[CollectionDefinition("api")] public sealed class ApiCollection : ICollectionFixture<ApiFixture>;
