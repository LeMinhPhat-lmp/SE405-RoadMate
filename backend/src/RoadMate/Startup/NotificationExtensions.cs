using RoadMate.Infrastructure.Notifications;

namespace RoadMate.Startup;

public static class NotificationExtensions
{
  /// <summary>Sms:Provider = esms -> eSMS thật; còn lại -> chỉ ghi log (an toàn cho dev).</summary>
  public static IServiceCollection AddSmsSender(this IServiceCollection services, IConfiguration cfg)
  {
    if (string.Equals(cfg["Sms:Provider"], "esms", StringComparison.OrdinalIgnoreCase))
      services.AddHttpClient<ISmsSender, ESmsSender>().RemoveAllLoggers();   // body chứa ApiKey/SecretKey
    else services.AddSingleton<ISmsSender, LogSmsSender>();
    return services;
  }
}
