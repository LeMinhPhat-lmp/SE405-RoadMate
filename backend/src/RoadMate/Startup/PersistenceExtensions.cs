using RoadMate.Repositories.Implements;

namespace RoadMate.Startup;

public static class PersistenceExtensions
{
  public static IServiceCollection AddRepositories(this IServiceCollection services) => services
    .AddScoped<IUserRepository, UserRepository>()
    .AddScoped<IEmergencyContactRepository, EmergencyContactRepository>()
    .AddScoped<ITripRepository, TripRepository>()
    .AddScoped<ITripPointRepository, TripPointRepository>()
    .AddScoped<IMemoryRepository, MemoryRepository>()
    .AddScoped<IAlertRepository, AlertRepository>()
    .AddScoped<IAlertDeliveryRepository, AlertDeliveryRepository>()
    .AddScoped<IAlertAckRepository, AlertAckRepository>()
    .AddScoped<IGroupRepository, GroupRepository>()
    .AddScoped<IGroupMemberRepository, GroupMemberRepository>()
    .AddScoped<IGroupMessageRepository, GroupMessageRepository>()
    .AddScoped<IUnitOfWork, UnitOfWork>();
}
