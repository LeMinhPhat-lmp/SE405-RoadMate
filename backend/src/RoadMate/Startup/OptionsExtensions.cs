using RoadMate.Infrastructure.Diagnostics;
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
    .Configure<DebugOptions>(cfg.GetSection("Debug"));
}
