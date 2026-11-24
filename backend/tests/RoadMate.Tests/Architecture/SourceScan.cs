using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace RoadMate.Tests.Architecture;

/// <summary>Static scan of the layer folders. Works without compiling (and survives the future split into 4 projects: just change the roots).</summary>
public static class SourceScan
{
  public static string Root { get; } = FindRoot();

  private static string FindRoot()
  {
    var env = Environment.GetEnvironmentVariable("ROADMATE_SRC");
    if (!string.IsNullOrEmpty(env)) return env;
    for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
    {
      var candidate = Path.Combine(dir.FullName, "src", "RoadMate");
      if (File.Exists(Path.Combine(candidate, "RoadMate.csproj"))) return candidate;
    }
    throw new InvalidOperationException("Không tìm thấy src/RoadMate (đặt biến ROADMATE_SRC).");
  }

  public sealed record SourceFile(string RelativePath, string Text, string[] Lines);

  public static IReadOnlyList<SourceFile> Layer(string layer) =>
    Directory.EnumerateFiles(Path.Combine(Root, layer), "*.cs", SearchOption.AllDirectories)
      .Where(p => !p.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}") && !p.Contains($"{Path.DirectorySeparatorChar}Migrations{Path.DirectorySeparatorChar}"))
      .Select(p => { var t = File.ReadAllText(p); return new SourceFile(Path.GetRelativePath(Root, p).Replace('\\', '/'), t, t.Split('\n')); }).ToList();

  public static IEnumerable<string> Usings(SourceFile f) =>
    f.Lines.Select(l => Regex.Match(l, @"^\s*(?:global\s+)?using\s+(?:static\s+)?([\w\.]+)\s*;")).Where(m => m.Success).Select(m => m.Groups[1].Value);

  /// <summary>Violations as "file: detail" lines; the test asserts the list is empty (so the failure message lists every offender).</summary>
  public static List<string> Violations(IEnumerable<SourceFile> files, Func<SourceFile, IEnumerable<string>> check) =>
    files.SelectMany(f => check(f).Select(d => $"{f.RelativePath}: {d}")).ToList();
}
