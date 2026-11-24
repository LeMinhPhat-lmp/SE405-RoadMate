using DotNetEnv;

namespace RoadMate.Api.Startup;

/// <summary>
/// Tìm và nạp file .env (.env.{Environment} và .env) bằng cách dò ngược từ các thư mục ứng dụng.
/// Dùng NoClobber để giữ nguyên biến môi trường đã đặt sẵn từ OS / Docker / launchSettings.
/// </summary>
public static class EnvLoader
{
  private static readonly StringComparer PathComparer =
    OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;

  public static IReadOnlyList<string> LoadForProcess(string[] args, params string?[] startDirectories)
  {
    var loaded = new List<string>();
    var seen = new HashSet<string>(PathComparer);
    var dirs = ResolveStartDirectories(startDirectories);
    var envName = ResolveEnvironmentName(args);

    if (!string.IsNullOrWhiteSpace(envName))
      TryLoadFile($".env.{envName}", loaded, seen, dirs);

    TryLoadFile(".env", loaded, seen, dirs);

    if (string.IsNullOrWhiteSpace(envName))
    {
      var resolvedEnv = ResolveEnvironmentName(args);
      if (!string.IsNullOrWhiteSpace(resolvedEnv))
        TryLoadFile($".env.{resolvedEnv}", loaded, seen, dirs);
    }

    return loaded;
  }

  public static IReadOnlyList<string> Load(string? environmentName, IEnumerable<string>? alreadyLoaded = null, params string?[] startDirectories)
  {
    var seen = new HashSet<string>(alreadyLoaded ?? [], PathComparer);
    var loaded = new List<string>();
    var dirs = ResolveStartDirectories(startDirectories);

    if (!string.IsNullOrWhiteSpace(environmentName))
      TryLoadFile($".env.{environmentName}", loaded, seen, dirs);

    TryLoadFile(".env", loaded, seen, dirs);
    return loaded;
  }

  public static string? ResolveEnvironmentName(string[] args)
  {
    for (var i = 0; i < args.Length; i++)
    {
      var arg = args[i];
      if (arg.Equals("--environment", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
        return args[i + 1];
      if (arg.StartsWith("--environment=", StringComparison.OrdinalIgnoreCase))
        return arg["--environment=".Length..];
    }

    return Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
      ?? Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT");
  }

  public static string? FindFileUpward(string fileName, params string?[] startDirectories)
  {
    var dirs = ResolveStartDirectories(startDirectories);
    var checkedDirs = new HashSet<string>(PathComparer);

    foreach (var start in dirs)
    {
      var dir = new DirectoryInfo(start);
      while (dir is not null)
      {
        if (checkedDirs.Add(dir.FullName))
        {
          var candidate = Path.Combine(dir.FullName, fileName);
          if (File.Exists(candidate))
            return Path.GetFullPath(candidate);
        }
        dir = dir.Parent;
      }
    }

    return null;
  }

  private static void TryLoadFile(string fileName, List<string> loaded, HashSet<string> seen, string[] dirs)
  {
    var path = FindFileUpward(fileName, dirs);
    if (path is null || !seen.Add(path)) return;

    Env.NoClobber().Load(path);
    loaded.Add(path);
  }

  private static string[] ResolveStartDirectories(string?[] startDirectories)
  {
    var candidates = startDirectories.Length > 0
      ? startDirectories
      : [Directory.GetCurrentDirectory(), AppContext.BaseDirectory];

    var result = new List<string>();
    var seen = new HashSet<string>(PathComparer);
    foreach (var dir in candidates)
    {
      if (string.IsNullOrWhiteSpace(dir) || !Directory.Exists(dir)) continue;
      var full = Path.GetFullPath(dir);
      if (seen.Add(full)) result.Add(full);
    }

    return [.. result];
  }
}
