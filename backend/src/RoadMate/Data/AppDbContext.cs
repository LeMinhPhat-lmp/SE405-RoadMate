namespace RoadMate.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

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
    public DbSet<Group> Groups => Set<Group>();
    public DbSet<GroupMember> GroupMembers => Set<GroupMember>();
    public DbSet<GroupMessage> GroupMessages => Set<GroupMessage>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<User>(e =>
        {
            e.HasKey(x => x.Uid);
            e.Property(x => x.Uid).HasMaxLength(128);
            e.HasIndex(x => x.Email).IsUnique();
            e.HasIndex(x => x.Phone);
        });

        b.Entity<EmergencyContact>(e =>
        {
            e.Property(x => x.Phone).HasMaxLength(20);
            e.HasOne(x => x.User).WithMany(u => u.EmergencyContacts).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.LinkedUser).WithMany().HasForeignKey(x => x.LinkedUserId).OnDelete(DeleteBehavior.SetNull);
            e.HasIndex(x => x.Phone);
        });

        b.Entity<Trip>(e =>
        {
            e.HasOne(x => x.Owner).WithMany(u => u.Trips).HasForeignKey(x => x.OwnerId);
            e.HasOne(x => x.Group).WithMany(g => g.Trips).HasForeignKey(x => x.GroupId).OnDelete(DeleteBehavior.SetNull);
            e.HasIndex(x => x.TrackingToken).IsUnique();
            e.HasIndex(x => new { x.OwnerId, x.Status });
        });

        b.Entity<TripWaypoint>(e => e.HasOne(x => x.Trip).WithMany(t => t.Waypoints).HasForeignKey(x => x.TripId));

        b.Entity<TripAlertRecipient>(e =>
        {
            e.HasKey(x => new { x.TripId, x.ContactId });
            e.HasOne(x => x.Trip).WithMany(t => t.Recipients).HasForeignKey(x => x.TripId);
            e.HasOne(x => x.Contact).WithMany().HasForeignKey(x => x.ContactId);
        });

        b.Entity<TripPoint>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).ValueGeneratedOnAdd();
            e.HasOne(x => x.Trip).WithMany(t => t.Points).HasForeignKey(x => x.TripId);
            e.HasIndex(x => new { x.TripId, x.RecordedAt });
        });

        b.Entity<TripLive>(e =>
        {
            e.HasKey(x => x.TripId);
            e.HasOne(x => x.Trip).WithOne(t => t.Live).HasForeignKey<TripLive>(x => x.TripId);
        });

        b.Entity<TripStats>(e =>
        {
            e.HasKey(x => x.TripId);
            e.HasOne(x => x.Trip).WithOne(t => t.Stats).HasForeignKey<TripStats>(x => x.TripId);
        });

        b.Entity<Memory>(e =>
        {
            e.HasOne(x => x.Trip).WithMany(t => t.Memories).HasForeignKey(x => x.TripId);
            e.HasIndex(x => new { x.TripId, x.TakenAt });
        });

        b.Entity<Alert>(e =>
        {
            e.HasOne(x => x.Trip).WithMany(t => t.Alerts).HasForeignKey(x => x.TripId);
            e.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId);
            e.HasIndex(x => x.ClientId).IsUnique();
            e.HasIndex(x => new { x.Status, x.NextRepeatAt });
        });

        b.Entity<AlertDelivery>(e =>
        {
            e.HasOne(x => x.Alert).WithMany(a => a.Deliveries).HasForeignKey(x => x.AlertId);
            e.HasOne(x => x.Contact).WithMany().HasForeignKey(x => x.ContactId).OnDelete(DeleteBehavior.SetNull);
        });

        b.Entity<Group>(e =>
        {
            e.HasIndex(x => x.Code).IsUnique();
            e.HasOne(x => x.Leader).WithMany().HasForeignKey(x => x.LeaderId).OnDelete(DeleteBehavior.Restrict);
        });
        b.Entity<GroupMember>(e =>
        {
            e.HasKey(x => new { x.GroupId, x.UserId });
            e.HasOne(x => x.Group).WithMany(g => g.Members).HasForeignKey(x => x.GroupId);
            e.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId);
        });
        b.Entity<GroupMessage>(e =>
        {
            e.HasOne(x => x.Group).WithMany(g => g.Messages).HasForeignKey(x => x.GroupId);
            e.HasOne(x => x.Sender).WithMany().HasForeignKey(x => x.SenderId);
        });
    }
}
