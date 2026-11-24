using System;
using System.IO;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using RoadMate.Api.Startup;
using RoadMate.Infrastructure.Firebase;

namespace RoadMate.Tests.Api;

public class EnvLoaderTests
{
  [Fact]
  public void Load_finds_env_file_in_parent_directory_and_does_not_clobber_existing_variables()
  {
    var root = Path.Combine(Path.GetTempPath(), $"roadmate-env-test-{Guid.NewGuid():N}");
    var nested = Path.Combine(root, "src", "RoadMate");
    Directory.CreateDirectory(nested);

    var keyFromEnvFile = $"ROADMATE_TEST_FROM_FILE_{Guid.NewGuid():N}";
    var keyAlreadySet = $"ROADMATE_TEST_EXISTING_{Guid.NewGuid():N}";
    var keyFallback = $"ROADMATE_TEST_FALLBACK_{Guid.NewGuid():N}";

    try
    {
      File.WriteAllText(Path.Combine(root, ".env.Development"),
        $"{keyFromEnvFile}=from-dev\n{keyAlreadySet}=from-dev\n");
      File.WriteAllText(Path.Combine(root, ".env"),
        $"{keyFromEnvFile}=from-base\n{keyFallback}=from-base\n");

      Environment.SetEnvironmentVariable(keyAlreadySet, "from-process");

      var loaded = EnvLoader.Load("Development", null, nested);

      Assert.Equal(2, loaded.Count);
      Assert.Equal("from-dev", Environment.GetEnvironmentVariable(keyFromEnvFile));
      Assert.Equal("from-process", Environment.GetEnvironmentVariable(keyAlreadySet));
      Assert.Equal("from-base", Environment.GetEnvironmentVariable(keyFallback));
    }
    finally
    {
      Environment.SetEnvironmentVariable(keyFromEnvFile, null);
      Environment.SetEnvironmentVariable(keyAlreadySet, null);
      Environment.SetEnvironmentVariable(keyFallback, null);
      if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
    }
  }

  [Fact]
  public void ResolveCredentialPath_resolves_root_relative_and_content_root_relative_paths()
  {
    var root = Path.Combine(Path.GetTempPath(), $"roadmate-fb-test-{Guid.NewGuid():N}");
    var contentRoot = Path.Combine(root, "src", "RoadMate");
    var secretDir = Path.Combine(contentRoot, "secrets");
    Directory.CreateDirectory(secretDir);
    var secretFile = Path.Combine(secretDir, "firebase.json");
    File.WriteAllText(secretFile, "{}");

    try
    {
      var fromRootRelative = FirebaseContext.ResolveCredentialPath("./src/RoadMate/secrets/firebase.json", contentRoot);
      var fromContentRootRelative = FirebaseContext.ResolveCredentialPath("secrets/firebase.json", contentRoot);
      var missing = FirebaseContext.ResolveCredentialPath("secrets/missing.json", contentRoot);

      Assert.Equal(Path.GetFullPath(secretFile), fromRootRelative);
      Assert.Equal(Path.GetFullPath(secretFile), fromContentRootRelative);
      Assert.Null(missing);
    }
    finally
    {
      if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
    }
  }

  [Fact]
  public void FirebaseContext_loads_real_development_service_account_from_nested_content_root()
  {
    var repoRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
    var contentRoot = Path.Combine(repoRoot, "src", "RoadMate");
    var envFile = Path.Combine(repoRoot, ".env.Development");
    if (!File.Exists(envFile)) return;

    var resolved = FirebaseContext.ResolveCredentialPath(
      "./src/RoadMate/secrets/se405-roadmate-firebase-adminsdk-fbsvc-db99da96a8.json",
      contentRoot);
    if (resolved is null) return;

    var fb = new FirebaseContext(
      Options.Create(new FirebaseOptions { CredentialPath = "./src/RoadMate/secrets/se405-roadmate-firebase-adminsdk-fbsvc-db99da96a8.json" }),
      NullLogger<FirebaseContext>.Instance);

    Assert.True(fb.IsConfigured);
    Assert.NotNull(fb.ServiceAccount);
  }
}
