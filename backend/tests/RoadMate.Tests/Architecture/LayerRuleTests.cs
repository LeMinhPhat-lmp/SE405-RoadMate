using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace RoadMate.Tests.Architecture;

/// <summary>Dependency and hygiene rules (P13). Each test names the rule so a failure explains itself.</summary>
public class LayerRuleTests
{
  private static void NoViolations(List<string> v) => Assert.True(v.Count == 0, "Vi phạm:\n  " + string.Join("\n  ", v));

  private static readonly string[] DomainForbiddenUsings =
    ["Microsoft.", "Npgsql", "Google.", "FirebaseAdmin", "Serilog", "System.Net.Http", "System.Text.Json", "RoadMate.Application", "RoadMate.Infrastructure", "RoadMate.Api"];
  private static readonly string[] DomainAllowedMicrosoft = [];

  [Fact, Trait("Scenario", "ARCH-01")]
  public void Domain_depends_on_nothing_but_the_BCL() => NoViolations(SourceScan.Violations(SourceScan.Layer("Domain"), f =>
    SourceScan.Usings(f).Where(u => DomainForbiddenUsings.Any(u.StartsWith)).Select(u => $"using {u}")));

  [Fact, Trait("Scenario", "ARCH-02")]
  public void Domain_entities_have_no_public_setters() => NoViolations(SourceScan.Violations(SourceScan.Layer("Domain"), f =>
    f.Lines.Select((l, i) => (l, i)).Where(t => Regex.IsMatch(t.l, @"public\s+[\w<>\?\[\],\. ]+\s+\w+\s*\{\s*get;\s*set;\s*\}")).Select(t => $"dòng {t.i + 1}: {t.l.Trim()}")));

  private static readonly string[] AppForbidden =
    ["Microsoft.EntityFrameworkCore", "Npgsql", "Microsoft.AspNetCore", "Google.", "FirebaseAdmin", "Serilog", "System.Net.Http", "RoadMate.Infrastructure", "RoadMate.Api"];

  [Fact, Trait("Scenario", "ARCH-03")]
  public void Application_does_not_reference_infrastructure_or_frameworks() => NoViolations(SourceScan.Violations(SourceScan.Layer("Application"), f =>
    SourceScan.Usings(f).Where(u => AppForbidden.Any(u.StartsWith)).Select(u => $"using {u}")));

  [Fact, Trait("Scenario", "ARCH-04")]
  public void Infrastructure_does_not_reference_api() => NoViolations(SourceScan.Violations(SourceScan.Layer("Infrastructure"), f =>
    SourceScan.Usings(f).Where(u => u.StartsWith("RoadMate.Api")).Select(u => $"using {u}")));

  [Fact, Trait("Scenario", "ARCH-05")]
  public void Only_the_composition_root_touches_infrastructure() => NoViolations(SourceScan.Violations(
    SourceScan.Layer("Api").Where(f => f.RelativePath != "Api/Program.cs" && !f.RelativePath.StartsWith("Api/Startup/")),
    f => SourceScan.Usings(f).Where(u => u.StartsWith("RoadMate.Infrastructure")).Select(u => $"using {u}")));

  [Fact, Trait("Scenario", "ARCH-06")]
  public void Controllers_depend_only_on_application_services_not_repositories_or_uow() => NoViolations(SourceScan.Violations(
    SourceScan.Layer("Api").Where(f => f.RelativePath.StartsWith("Api/Controllers/")),
    f => Regex.Matches(f.Text, @"\b(I\w+Repository|IUnitOfWork|AppDbContext|I\w+ReadModel)\b").Select(m => m.Value).Distinct()));

  [Fact, Trait("Scenario", "ARCH-07")]
  public void No_direct_clock_access_outside_TimeProvider() => NoViolations(SourceScan.Violations(
    SourceScan.Layer("Domain").Concat(SourceScan.Layer("Application")).Concat(SourceScan.Layer("Api")),
    f => f.Lines.Select((l, i) => (l, i)).Where(t => !t.l.TrimStart().StartsWith("//") && !t.l.TrimStart().StartsWith("///") && Regex.IsMatch(t.l, @"\b(DateTime|DateTimeOffset)\.(UtcNow|Now|Today)\b"))
      .Select(t => $"dòng {t.i + 1}: {t.l.Trim()}")));

  private static readonly HashSet<string> NotCollaborators = ["TimeProvider", "ILogger", "IOptions", "IOptionsMonitor", "IMemoryCache", "IServiceProvider", "CancellationToken"];

  [Fact, Trait("Scenario", "ARCH-08")]
  public void Services_have_at_most_five_collaborators() => NoViolations(SourceScan.Violations(
    SourceScan.Layer("Application").Where(f => Regex.IsMatch(f.RelativePath, @"(Service|Processor|Raiser|Planner|Checker|Builder)\.cs$")),
    f => Regex.Matches(f.Text, @"public\s+(?:sealed\s+)?class\s+(\w+)\s*\(([^)]*)\)").Select(m =>
    {
      var parts = Regex.Split(m.Groups[2].Value, @",(?![^<]*>)").Select(p => p.Trim()).Where(p => p.Length > 0).ToList();
      var n = parts.Count(p => !NotCollaborators.Any(x => p.StartsWith(x)) && !p.StartsWith("Alert") || p.StartsWith("AlertTiming"));
      return (Name: m.Groups[1].Value, Count: parts.Count(p => !NotCollaborators.Any(x => p.StartsWith(x))));
    }).Where(t => t.Count > 5).Select(t => $"{t.Name} có {t.Count} collaborator (tối đa 5, không tính TimeProvider/ILogger/IOptions)")));

  [Fact, Trait("Scenario", "ARCH-09")]
  public void Source_files_stay_small() => NoViolations(SourceScan.Violations(
    SourceScan.Layer("Domain").Concat(SourceScan.Layer("Application")),
    f => f.Lines.Length > 260 ? [$"{f.Lines.Length} dòng (tối đa 260; tách theo trách nhiệm)"] : []));

  [Fact, Trait("Scenario", "ARCH-10")]
  public void No_project_wide_global_usings_of_layer_namespaces() => NoViolations(SourceScan.Violations(
    new[] { "Domain", "Application", "Infrastructure", "Api" }.SelectMany(SourceScan.Layer),
    f => f.Lines.Where(l => Regex.IsMatch(l, @"^\s*global\s+using\s+RoadMate\.")).Select(l => l.Trim())));
}
