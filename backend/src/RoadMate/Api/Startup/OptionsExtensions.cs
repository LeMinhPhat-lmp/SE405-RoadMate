using RoadMate.Application.Abstractions.Storage;
using RoadMate.Application.Alerts;
using RoadMate.Application.Groups;
using RoadMate.Application.Maps;
using RoadMate.Application.Monitoring;
using RoadMate.Application.Tracking;
using RoadMate.Application.Weather;
using RoadMate.Infrastructure.Maps;
using RoadMate.Infrastructure.Notifications;
using RoadMate.Infrastructure.Firebase;
using RoadMate.Api.Logging;

namespace RoadMate.Api.Startup;

public static class OptionsExtensions
{
  /// <summary>Đăng ký mọi *Options và section cấu hình tương ứng trong appsettings.</summary>
  public static IServiceCollection AddAppOptions(this IServiceCollection services, IConfiguration cfg) => services
    .Configure<AlertOptions>(cfg.GetSection("Alert"))
    .Configure<TrackingOptions>(cfg.GetSection("Tracking"))
    .Configure<GoongOptions>(cfg.GetSection("Goong"))
    .Configure<MapOptions>(cfg.GetSection("Goong"))
    .Configure<SmsOptions>(cfg.GetSection("Sms"))
    .Configure<DebugOptions>(cfg.GetSection("Debug"))
    .Configure<SafetyOptions>(cfg.GetSection("Safety"))
    .Configure<GroupOptions>(cfg.GetSection("Groups"))
    .Configure<WeatherOptions>(cfg.GetSection("Weather"))
    .Configure<StorageOptions>(cfg.GetSection("Storage"))
    .Configure<FirebaseOptions>(cfg.GetSection("Firebase"))
    .Configure<ObservabilityOptions>(cfg.GetSection(ObservabilityOptions.Section));
}
