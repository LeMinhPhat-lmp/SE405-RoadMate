namespace RoadMate.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options, ICurrentUserContext currentUser) : DbContext(options)
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
  public DbSet<AlertDelivery> AlertDeliveries => Set<AlertDelivery>();
  public DbSet<AlertAck> AlertAcks => Set<AlertAck>();
  public DbSet<Group> Groups => Set<Group>();
  public DbSet<GroupMember> GroupMembers => Set<GroupMember>();
  public DbSet<GroupMessage> GroupMessages => Set<GroupMessage>();

  /// <summary>Cấu hình từng entity nằm trong Data/Configurations; ở đây chỉ gom lại.</summary>
  protected override void OnModelCreating(ModelBuilder b) =>
    b.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly).ApplySoftDeleteQueryFilters();

  public override int SaveChanges(bool acceptAllChangesOnSuccess)
  {
    ChangeTracker.ApplyAudit(currentUser.UserId ?? AuditConstants.SystemUserId, DateTimeOffset.UtcNow);
    return base.SaveChanges(acceptAllChangesOnSuccess);
  }

  public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
  {
    ChangeTracker.ApplyAudit(currentUser.UserId ?? AuditConstants.SystemUserId, DateTimeOffset.UtcNow);
    return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
  }
}
