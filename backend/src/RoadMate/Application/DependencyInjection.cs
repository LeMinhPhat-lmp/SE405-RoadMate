using RoadMate.Application.Accounts.Validators;
using Microsoft.Extensions.Options;
using RoadMate.Application.Abstractions.Messaging;
using RoadMate.Application.Accounts;
using RoadMate.Application.Accounts.DTOs.Requests;
using RoadMate.Application.Accounts.Implements;
using RoadMate.Application.Accounts.Interfaces;
using RoadMate.Application.Alerts;
using RoadMate.Application.Alerts.DTOs.Requests;
using RoadMate.Application.Alerts.Implements;
using RoadMate.Application.Alerts.Interfaces;
using RoadMate.Application.Alerts.Validators;
using RoadMate.Application.Contacts.DTOs.Requests;
using RoadMate.Application.Contacts.Implements;
using RoadMate.Application.Contacts.Interfaces;
using RoadMate.Application.Contacts.Validators;
using RoadMate.Application.Groups.DTOs.Requests;
using RoadMate.Application.Groups.Implements;
using RoadMate.Application.Groups.Interfaces;
using RoadMate.Application.Groups.Validators;
using RoadMate.Application.Handlers;
using RoadMate.Application.Journal.DTOs.Requests;
using RoadMate.Application.Journal.Implements;
using RoadMate.Application.Journal.Interfaces;
using RoadMate.Application.Journal.Validators;
using RoadMate.Application.Maps.Implements;
using RoadMate.Application.Maps.Interfaces;
using RoadMate.Application.Monitoring.Implements;
using RoadMate.Application.Monitoring.Interfaces;
using RoadMate.Application.Notifications;
using RoadMate.Application.Tracking.DTOs.Requests;
using RoadMate.Application.Tracking.Implements;
using RoadMate.Application.Tracking.Interfaces;
using RoadMate.Application.Tracking.Validators;
using RoadMate.Application.Trips.DTOs.Requests;
using RoadMate.Application.Trips.Implements;
using RoadMate.Application.Trips.Interfaces;
using RoadMate.Application.Trips.Validators;
using RoadMate.Application.Weather.Implements;
using RoadMate.Application.Weather.Interfaces;
using RoadMate.Domain.Alerts;
using RoadMate.Domain.Events;

namespace RoadMate.Application;

/// <summary>Registers every Application service. Infrastructure supplies the ports; Api supplies options binding and the composition root.</summary>
public static class DependencyInjection
{
  public static IServiceCollection AddApplication(this IServiceCollection s)
  {
    // Domain policies (OCP: one class per alert type) and timing derived from options
    foreach (var policy in AlertPolicyResolver.DefaultPolicies()) s.AddSingleton(typeof(IAlertTypePolicy), policy);
    s.AddSingleton<IAlertPolicyResolver, AlertPolicyResolver>();
    s.AddSingleton(sp => sp.GetRequiredService<IOptions<AlertOptions>>().Value.Timing);

    // Request validators
    s.AddSingleton<IRequestValidator<UpdateProfileRequest>, UpdateProfileRequestValidator>();
    s.AddSingleton<IRequestValidator<ContactRequest>, ContactRequestValidator>();
    s.AddSingleton<IRequestValidator<CreateAlertRequest>, CreateAlertRequestValidator>();
    s.AddSingleton<IRequestValidator<TripUpsertRequest>, TripUpsertRequestValidator>();
    s.AddSingleton<IRequestValidator<RestRequest>, RestRequestValidator>();
    s.AddSingleton<IRequestValidator<AddPointsRequest>, AddPointsRequestValidator>();
    s.AddSingleton<IRequestValidator<HeartbeatRequest>, HeartbeatRequestValidator>();
    s.AddSingleton<IRequestValidator<MemoryRequest>, MemoryRequestValidator>();
    s.AddSingleton<IRequestValidator<CreateGroupRequest>, CreateGroupRequestValidator>();
    s.AddSingleton<IRequestValidator<JoinGroupRequest>, JoinGroupRequestValidator>();
    s.AddSingleton<IRequestValidator<SendGroupMessageRequest>, SendGroupMessageRequestValidator>();

    // Services (scoped: one DbContext per request)
    s.AddScoped<IAuthService, AuthService>();
    s.AddScoped<IEmergencyContactService, EmergencyContactService>();
    s.AddScoped<IContactTestService, ContactTestService>();
    s.AddScoped<ITripService, TripService>();
    s.AddScoped<ITripLifecycleService, TripLifecycleService>();
    s.AddScoped<ITripRouteService, TripRouteService>();
    s.AddScoped<ITelemetryService, TelemetryService>();
    s.AddScoped<ITrackingLinkService, TrackingLinkService>();
    s.AddScoped<IPublicTrackingService, PublicTrackingService>();
    s.AddScoped<ISimulationGate, SimulationGate>();
    s.AddScoped<INotificationPlanner, NotificationPlanner>();
    s.AddScoped<IAlertRaiser, AlertRaiser>();
    s.AddScoped<IAlertService, AlertService>();
    s.AddScoped<IAlertLifecycleService, AlertLifecycleService>();
    s.AddScoped<IAlertSimulationService, AlertSimulationService>();
    s.AddScoped<IAlertAckService, AlertAckService>();
    s.AddScoped<IDeliveryMessageBuilder, DeliveryMessageBuilder>();
    s.AddScoped<IOutboxProcessor, OutboxProcessor>();
    s.AddScoped<IAlertRepeatProcessor, AlertRepeatProcessor>();
    s.AddScoped<IAlertRepeatService, AlertRepeatService>();
    s.AddScoped<ITripMonitorService, TripMonitorService>();
    s.AddScoped<ITripCheckService, TripCheckService>();
    s.AddScoped<IGroupService, GroupService>();
    s.AddScoped<IGroupMessageService, GroupMessageService>();
    s.AddScoped<IGroupMonitorService, GroupMonitorService>();
    s.AddScoped<IJournalService, JournalService>();
    s.AddScoped<IMemoryService, MemoryService>();
    s.AddScoped<IMediaService, MediaService>();
    s.AddScoped<IMediaReferenceChecker, MediaReferenceChecker>();
    s.AddScoped<IRoutingService, RoutingService>();
    s.AddScoped<IPlaceService, PlaceService>();
    s.AddScoped<IGeocodingService, GeocodingService>();
    s.AddScoped<IWeatherService, WeatherService>();
    s.AddScoped<IWeatherWatchService, WeatherWatchService>();
    s.AddScoped<IPushNotifier, PushNotifier>();

    // Domain-event handlers: adding behaviour = adding a class here
    s.AddScoped<IDomainEventHandler<AlertClosed>, AlertClosedPlansSafeNotice>();
    s.AddScoped<IDomainEventHandler<TripFinished>, TripFinishedClosesAlerts>();
    s.AddScoped<IDomainEventHandler<ExpectedReturnExtended>, ExpectedReturnExtendedClosesLateReturn>();
    s.AddScoped<IDomainEventHandler<LocationReported>, LocationReportedClosesLostSignal>();
    s.AddScoped<IDomainEventHandler<LocationReported>, LocationReportedUpdatesGroupMember>();
    s.AddScoped<IDomainEventHandler<MemberRemovedFromGroup>, MemberRemovedDetachesTrips>();
    return s;
  }
}
