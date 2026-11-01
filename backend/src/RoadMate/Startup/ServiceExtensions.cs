using RoadMate.Infrastructure.Background;
using RoadMate.Infrastructure.Maps;
using RoadMate.Services.Notifications;

namespace RoadMate.Startup;

public static class ServiceExtensions
{
  /// <summary>Đăng ký service theo từng ranh giới (feature) + worker nền.</summary>
  public static IServiceCollection AddFeatureServices(this IServiceCollection services) => services
    // Accounts + Contacts
    .AddScoped<IAuthService, AuthService>()
    .AddScoped<IEmergencyContactService, EmergencyContactService>()
    // Trips
    .AddScoped<ITripService, TripService>()
    .AddScoped<ITripLifecycleService, TripLifecycleService>()
    // Tracking
    .AddScoped<ITelemetryService, TelemetryService>()
    .AddScoped<ITrackingLinkService, TrackingLinkService>()
    .AddScoped<IPublicTrackingService, PublicTrackingService>()
    // Journal
    .AddScoped<IJournalService, JournalService>()
    .AddScoped<IMemoryService, MemoryService>()
    // Alerts + Notifications
    .AddScoped<IContactNotifier, ContactNotifier>()
    .AddScoped<IAlertDispatcher, AlertDispatcher>()
    .AddScoped<IAlertService, AlertService>()
    .AddScoped<IAlertRepeatProcessor, AlertRepeatProcessor>()
    .AddHostedService<AlertRepeatWorker>()
    // Maps (Goong)
    .AddHttpClient<IGoongClient, GoongClient>().Services
    .AddScoped<IRoutingService, RoutingService>()
    .AddScoped<IPlaceService, PlaceService>();
}
