using Microsoft.EntityFrameworkCore;
using RoadMate.Application.Abstractions.Identity;
using RoadMate.Domain.Alerts;
using RoadMate.Domain.Common;
using RoadMate.Domain.Groups;
using RoadMate.Domain.Trips;
using RoadMate.Domain.Users;

namespace RoadMate.Infrastructure.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options, ICurrentUser currentUser, TimeProvider clock) : DbContext(options)
{
  public DbSet<User> Users => Set<User>();
  public DbSet<EmergencyContact> EmergencyContacts => Set<EmergencyContact>();
  public DbSet<Trip> Trips => Set<Trip>();
  public DbSet<TripWaypoint> TripWaypoints => Set<TripWaypoint>();
  public DbSet<TripAlertRecipient> TripAlertRecipients => Set<TripAlertRecipient>();
  public DbSet<TripPoint> TripPoints => Set<TripPoint>();
  public DbSet<TripLive> TripLives => Set<TripLive>();
  public DbSet<TripStats> Stats => Set<TripStats>();
  public DbSet<Memory> Memories => Set<Memory>();
  public DbSet<Alert> Alerts => Set<Alert>();
  public DbSet<NotificationDelivery> NotificationDeliveries => Set<NotificationDelivery>();
  public DbSet<AlertAck> AlertAcks => Set<AlertAck>();
  public DbSet<Group> Groups => Set<Group>();
  public DbSet<GroupMember> GroupMembers => Set<GroupMember>();
  public DbSet<GroupMessage> GroupMessages => Set<GroupMessage>();

  protected override void OnModelCreating(ModelBuilder b) =>
    b.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly).ApplySoftDeleteQueryFilters();

  /// <summary>Enums are stored as their legacy wire strings (text columns), never as ints.</summary>
  protected override void ConfigureConventions(ModelConfigurationBuilder c)
  {
    c.Properties<AlertType>().HaveConversion<WireConverter<AlertType>>().HaveMaxLength(20);
    c.Properties<AlertStatus>().HaveConversion<WireConverter<AlertStatus>>().HaveMaxLength(20);
    c.Properties<AlertResolver>().HaveConversion<WireConverter<AlertResolver>>().HaveMaxLength(20);
    c.Properties<SimulationMode>().HaveConversion<WireConverter<SimulationMode>>().HaveMaxLength(20);
    c.Properties<NotificationKind>().HaveConversion<WireConverter<NotificationKind>>().HaveMaxLength(20);
    c.Properties<DeliveryChannel>().HaveConversion<WireConverter<DeliveryChannel>>().HaveMaxLength(20);
    c.Properties<DeliveryStatus>().HaveConversion<WireConverter<DeliveryStatus>>().HaveMaxLength(20);
    c.Properties<TripStatus>().HaveConversion<WireConverter<TripStatus>>().HaveMaxLength(20);
    c.Properties<MemoryType>().HaveConversion<WireConverter<MemoryType>>().HaveMaxLength(20);
    c.Properties<CrashSensitivity>().HaveConversion<WireConverter<CrashSensitivity>>().HaveMaxLength(20);
    c.Properties<GroupStatus>().HaveConversion<WireConverter<GroupStatus>>().HaveMaxLength(20);
    c.Properties<GroupRole>().HaveConversion<WireConverter<GroupRole>>().HaveMaxLength(20);
  }

  public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
  {
    ChangeTracker.ApplyAudit(currentUser.UserId ?? AuditConstants.SystemUserId, clock.GetUtcNow());
    return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
  }

  public override int SaveChanges(bool acceptAllChangesOnSuccess)
  {
    ChangeTracker.ApplyAudit(currentUser.UserId ?? AuditConstants.SystemUserId, clock.GetUtcNow());
    return base.SaveChanges(acceptAllChangesOnSuccess);
  }
}
