using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using Google.Cloud.Storage.V1;
using RoadMate.Infrastructure.Firebase;
using RoadMate.Infrastructure.Notifications;
using RoadMate.Infrastructure.Storage;
using Serilog;

namespace RoadMate.Startup;

public static class FirebaseExtensions
{
  public static bool AddFirebase(this IServiceCollection services, IConfiguration cfg)
  {
    var firebase = cfg.GetSection("Firebase").Get<FirebaseOptions>() ?? new FirebaseOptions();
    var credential = TryLoadServiceAccount(firebase.CredentialPath, out var serviceAccount);
    if (credential is null || serviceAccount is null)
    {
      services.AddSingleton<IPushSender, LogPushSender>();
      services.AddSingleton<IMediaStorage, UnconfiguredMediaStorage>();
      return false;
    }

    FirebaseApp.Create(new AppOptions { Credential = credential });
    services.AddSingleton<IPushSender, FcmPushSender>();
    services.AddSingleton<IMediaStorage>(sp => new GcsMediaStorage(
      serviceAccount, StorageClient.Create(credential), sp.GetRequiredService<IOptions<StorageOptions>>(),
      sp.GetRequiredService<TimeProvider>(), sp.GetRequiredService<ILogger<GcsMediaStorage>>()));
    return true;
  }

  private static GoogleCredential? TryLoadServiceAccount(string? path, out ServiceAccountCredential? serviceAccount)
  {
    serviceAccount = null;
    if (string.IsNullOrWhiteSpace(path)) return null;
    if (!File.Exists(path))
    {
      Log.Warning("[CONFIG] Firebase:CredentialPath trỏ tới file không tồn tại: {Path}", path);
      return null;
    }
    try
    {
      var credential = GoogleCredential.FromFile(path);
      serviceAccount = credential.UnderlyingCredential as ServiceAccountCredential;
      if (serviceAccount is null) Log.Error("[CONFIG] File {Path} không phải service account (cần để ký URL Storage).", path);
      return serviceAccount is null ? null : credential;
    }
    catch (Exception ex)
    {
      // Không log nội dung file: có thể chứa private key.
      Log.Error("[CONFIG] Không đọc được service account tại {Path}: {Reason}", path, ex.GetType().Name);
      return null;
    }
  }
#pragma warning restore CS0618
}
