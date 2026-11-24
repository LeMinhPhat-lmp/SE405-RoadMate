using Google.Cloud.Storage.V1;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RoadMate.Application.Abstractions.Maps;
using RoadMate.Application.Abstractions.Messaging;
using RoadMate.Application.Abstractions.Persistence;
using RoadMate.Application.Abstractions.Storage;
using RoadMate.Application.Abstractions.Weather;
using RoadMate.Infrastructure.Background;
using RoadMate.Infrastructure.Firebase;
using RoadMate.Infrastructure.Maps;
using RoadMate.Infrastructure.Notifications;
using RoadMate.Infrastructure.Persistence;
using RoadMate.Infrastructure.Persistence.Repositories;
using RoadMate.Infrastructure.Storage;
using RoadMate.Infrastructure.Weather;

namespace RoadMate.Infrastructure;

/// <summary>Registers every port implementation. Called once by the Api composition root.</summary>
public static class DependencyInjection
{
  public static IServiceCollection AddInfrastructure(this IServiceCollection s, IConfiguration cfg)
  {
    s.AddDbContext<AppDbContext>(o => o.UseNpgsql(cfg.GetConnectionString("Default")));

    s.AddScoped<IUnitOfWork, UnitOfWork>();
    s.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();
    s.AddScoped<IUserRepository, UserRepository>();
    s.AddScoped<IEmergencyContactRepository, EmergencyContactRepository>();
    s.AddScoped<ITripRepository, TripRepository>();
    s.AddScoped<IMemoryRepository, MemoryRepository>();
    s.AddScoped<IAlertRepository, AlertRepository>();
    s.AddScoped<IAlertAckRepository, AlertAckRepository>();
    s.AddScoped<INotificationDeliveryRepository, NotificationDeliveryRepository>();
    s.AddScoped<IGroupRepository, GroupRepository>();
    s.AddScoped<IGroupMessageRepository, GroupMessageRepository>();
    s.AddScoped<TripTelemetryStore>();
    s.AddScoped<ITripTelemetryWriter>(sp => sp.GetRequiredService<TripTelemetryStore>());
    s.AddScoped<ITripPointReader>(sp => sp.GetRequiredService<TripTelemetryStore>());
    s.AddScoped<IGroupMemberLocationWriter, GroupMemberLocationWriter>();
    s.AddScoped<IAlertReadModel, AlertReadModel>();
    s.AddScoped<IGroupReadModel, GroupReadModel>();
    s.AddScoped<IPublicTrackingReadModel, PublicTrackingReadModel>();

    s.AddSingleton<IIsolatedExecutor, ScopedExecutor>();
    s.AddSingleton<ICooldown, MemoryCooldown>();
    s.AddMemoryCache();
    s.AddHealthChecks().AddDbContextCheck<AppDbContext>("db", tags: ["ready"]);

    // Firebase: one context decides between the real FCM/GCS adapters and the safe log-only/503 ones.
    s.AddSingleton<FirebaseContext>();
    s.AddSingleton<IPushProvider>(sp => sp.GetRequiredService<FirebaseContext>().IsConfigured
      ? ActivatorUtilities.CreateInstance<FcmPushSender>(sp) : ActivatorUtilities.CreateInstance<LogPushSender>(sp));
    s.AddSingleton<IMediaStorage>(sp =>
    {
      var fb = sp.GetRequiredService<FirebaseContext>();
      return fb.IsConfigured
        ? new GcsMediaStorage(fb.ServiceAccount!, StorageClient.Create(fb.Credential), sp.GetRequiredService<IOptions<StorageOptions>>(), sp.GetRequiredService<TimeProvider>(), sp.GetRequiredService<ILogger<GcsMediaStorage>>())
        : new UnconfiguredMediaStorage();
    });
    s.AddSingleton<INotificationChannel, SmsChannel>();
    s.AddSingleton<INotificationChannel, PushChannel>();

    // SMS provider: Sms:Provider=esms → real gateway, anything else → log only (safe for dev). The body contains keys: HTTP logging removed.
    if (string.Equals(cfg["Sms:Provider"], "esms", StringComparison.OrdinalIgnoreCase)) s.AddHttpClient<ISmsProvider, ESmsSender>().RemoveAllLoggers();
    else s.AddSingleton<ISmsProvider, LogSmsSender>();

    // The URL carries api_key: no HTTP logging for these clients.
    s.AddHttpClient<IMapGateway, GoongClient>().RemoveAllLoggers();
    s.AddHttpClient<IWeatherClient, OpenMeteoClient>();
    return s;
  }
}
