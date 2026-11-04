using RoadMate.Infrastructure.Diagnostics;
using RoadMate.Infrastructure.Firebase;
using RoadMate.Infrastructure.Logging;
using RoadMate.Infrastructure.Storage;
using RoadMate.Infrastructure.Weather;
using RoadMate.Services.Groups;
using RoadMate.Services.Monitoring;
using RoadMate.Infrastructure.Maps;
using RoadMate.Infrastructure.Notifications;

namespace RoadMate.Startup;

public static class OptionsExtensions
{
  /// <summary>Đăng ký mọi *Options và section cấu hình tương ứng trong appsettings.</summary>
  public static IServiceCollection AddAppOptions(this IServiceCollection services, IConfiguration cfg) => services
    .Configure<AlertOptions>(cfg.GetSection("Alert"))
    .Configure<TrackingOptions>(cfg.GetSection("Tracking"))
    .Configure<GoongOptions>(cfg.GetSection("Goong"))
    .Configure<SmsOptions>(cfg.GetSection("Sms"))
    .Configure<DebugOptions>(cfg.GetSection("Debug"))
    .Configure<SafetyOptions>(cfg.GetSection("Safety"))
    .Configure<GroupOptions>(cfg.GetSection("Groups"))
    .Configure<WeatherOptions>(cfg.GetSection("Weather"))
    .Configure<StorageOptions>(cfg.GetSection("Storage"))
    .Configure<FirebaseOptions>(cfg.GetSection("Firebase"))
    .Configure<ObservabilityOptions>(cfg.GetSection(ObservabilityOptions.Section));
}
