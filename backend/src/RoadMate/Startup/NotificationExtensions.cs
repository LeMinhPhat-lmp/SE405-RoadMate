using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using RoadMate.Infrastructure.Notifications;

namespace RoadMate.Startup;

public static class NotificationExtensions
{
  /// <summary>Firebase Admin (FCM). Không có credential thì dùng LogPushSender. Trả về true nếu đã nạp credential.</summary>
  public static bool AddFirebasePush(this IServiceCollection services, IConfiguration cfg)
  {
    var credPath = cfg["Firebase:CredentialPath"];
    var loaded = !string.IsNullOrWhiteSpace(credPath) && File.Exists(credPath);
    if (loaded)
    {
      FirebaseApp.Create(new AppOptions { Credential = GoogleCredential.FromFile(credPath) });
      services.AddSingleton<IPushSender, FcmPushSender>();
    }
    else services.AddSingleton<IPushSender, LogPushSender>();
    return loaded;
  }

  public static IServiceCollection AddSmsSender(this IServiceCollection services, IConfiguration cfg)
  {
    if (string.Equals(cfg["Sms:Provider"], "esms", StringComparison.OrdinalIgnoreCase)) services.AddHttpClient<ISmsSender, ESmsSender>();
    else services.AddSingleton<ISmsSender, LogSmsSender>();
    return services;
  }
}
