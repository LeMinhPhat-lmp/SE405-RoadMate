using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace RoadMate.Infrastructure.Firebase;

/// <summary>
/// Loads the service-account file once (shared by FCM and Storage URL signing). Never logs file contents (they contain a private key).
/// Not configured → push is log-only and media endpoints answer 503 STORAGE_NOT_CONFIGURED.
/// </summary>
public sealed class FirebaseContext
{
  public GoogleCredential? Credential { get; }
  public ServiceAccountCredential? ServiceAccount { get; }
  public bool IsConfigured => Credential is not null && ServiceAccount is not null;

  public FirebaseContext(IOptions<FirebaseOptions> options, ILogger<FirebaseContext> log, IHostEnvironment? env = null)
  {
    var configuredPath = options.Value.CredentialPath;
    if (string.IsNullOrWhiteSpace(configuredPath)) return;

    var resolvedPath = ResolveCredentialPath(configuredPath, env?.ContentRootPath);
    if (resolvedPath is null) { log.LogWarning("[CONFIG] Firebase:CredentialPath trỏ tới file không tồn tại: {Path}", configuredPath); return; }
    try
    {
      var credential = GoogleCredential.FromFile(resolvedPath);
      ServiceAccount = credential.UnderlyingCredential as ServiceAccountCredential;
      if (ServiceAccount is null) { log.LogError("[CONFIG] File {Path} không phải service account (cần để ký URL Storage).", resolvedPath); return; }
      Credential = credential;
      if (FirebaseApp.DefaultInstance is null) FirebaseApp.Create(new AppOptions { Credential = credential });
    }
    catch (Exception ex)
    {
      log.LogError("[CONFIG] Không đọc được service account tại {Path}: {Reason}", resolvedPath, ex.GetType().Name);
    }
  }

  public static string? ResolveCredentialPath(string path, string? contentRootPath = null)
  {
    if (string.IsNullOrWhiteSpace(path)) return null;
    if (Path.IsPathRooted(path)) return File.Exists(path) ? Path.GetFullPath(path) : null;

    var comparer = OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;
    var checkedDirs = new HashSet<string>(comparer);

    foreach (var start in new[] { contentRootPath, Directory.GetCurrentDirectory(), AppContext.BaseDirectory })
    {
      if (string.IsNullOrWhiteSpace(start) || !Directory.Exists(start)) continue;
      var dir = new DirectoryInfo(Path.GetFullPath(start));
      while (dir is not null)
      {
        if (checkedDirs.Add(dir.FullName))
        {
          var candidate = Path.GetFullPath(Path.Combine(dir.FullName, path));
          if (File.Exists(candidate)) return candidate;
        }
        dir = dir.Parent;
      }
    }

    return null;
  }
}
