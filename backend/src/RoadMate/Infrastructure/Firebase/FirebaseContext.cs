using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
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

  public FirebaseContext(IOptions<FirebaseOptions> options, ILogger<FirebaseContext> log)
  {
    var path = options.Value.CredentialPath;
    if (string.IsNullOrWhiteSpace(path)) return;
    if (!File.Exists(path)) { log.LogWarning("[CONFIG] Firebase:CredentialPath trỏ tới file không tồn tại: {Path}", path); return; }
    try
    {
      var credential = GoogleCredential.FromFile(path);
      ServiceAccount = credential.UnderlyingCredential as ServiceAccountCredential;
      if (ServiceAccount is null) { log.LogError("[CONFIG] File {Path} không phải service account (cần để ký URL Storage).", path); return; }
      Credential = credential;
      if (FirebaseApp.DefaultInstance is null) FirebaseApp.Create(new AppOptions { Credential = credential });
    }
    catch (Exception ex)
    {
      log.LogError("[CONFIG] Không đọc được service account tại {Path}: {Reason}", path, ex.GetType().Name);
    }
  }
}
