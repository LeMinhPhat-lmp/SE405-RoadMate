using System.Linq.Expressions;

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
  public DbSet<Group> Groups => Set<Group>();
  public DbSet<GroupMember> GroupMembers => Set<GroupMember>();
  public DbSet<GroupMessage> GroupMessages => Set<GroupMessage>();

  private const string NotDeleted = "\"DeletedAt\" IS NULL";

  protected override void OnModelCreating(ModelBuilder b)
  {
    b.Entity<User>(e =>
    {
      e.Property(x => x.FirebaseUid).HasMaxLength(128).IsRequired();
      e.HasIndex(x => x.FirebaseUid).IsUnique().HasFilter(NotDeleted);
      e.HasIndex(x => x.Email).IsUnique().HasFilter(NotDeleted);
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
      e.HasOne(x => x.Trip).WithMany(t => t.Recipients).HasForeignKey(x => x.TripId);
      e.HasOne(x => x.Contact).WithMany().HasForeignKey(x => x.ContactId);
      e.HasIndex(x => new { x.TripId, x.ContactId }).IsUnique();
    });

    b.Entity<TripPoint>(e =>
    {
      e.HasOne(x => x.Trip).WithMany(t => t.Points).HasForeignKey(x => x.TripId);
      e.HasIndex(x => new { x.TripId, x.RecordedAt });
    });

    b.Entity<TripLive>(e => e.HasOne(x => x.Trip).WithOne(t => t.Live).HasForeignKey<TripLive>(x => x.TripId));
    b.Entity<TripStats>(e => e.HasOne(x => x.Trip).WithOne(t => t.Stats).HasForeignKey<TripStats>(x => x.TripId));

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
      e.HasIndex(x => x.Code).IsUnique().HasFilter(NotDeleted);
      e.HasOne(x => x.Leader).WithMany().HasForeignKey(x => x.LeaderId).OnDelete(DeleteBehavior.Restrict);
    });
    b.Entity<GroupMember>(e =>
    {
      e.HasOne(x => x.Group).WithMany(g => g.Members).HasForeignKey(x => x.GroupId);
      e.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId);
      e.HasIndex(x => new { x.GroupId, x.UserId }).IsUnique();
    });
    b.Entity<GroupMessage>(e =>
    {
      e.HasOne(x => x.Group).WithMany(g => g.Messages).HasForeignKey(x => x.GroupId);
      e.HasOne(x => x.Sender).WithMany().HasForeignKey(x => x.SenderId);
    });

    ApplySoftDeleteQueryFilters(b);
  }

  /// <summary>Mọi entity ISoftDeletableEntity tự động ẩn bản ghi đã xoá mềm (dùng IgnoreQueryFilters() để xem lại).</summary>
  private static void ApplySoftDeleteQueryFilters(ModelBuilder b)
  {
    foreach (var et in b.Model.GetEntityTypes().Where(t => typeof(ISoftDeletableEntity).IsAssignableFrom(t.ClrType)))
    {
      var p = Expression.Parameter(et.ClrType, "e");
      var deletedAt = Expression.Property(p, nameof(ISoftDeletableEntity.DeletedAt));
      var body = Expression.Equal(deletedAt, Expression.Constant(null, typeof(DateTimeOffset?)));
      et.SetQueryFilter(Expression.Lambda(body, p));
    }
  }

  public override int SaveChanges(bool acceptAllChangesOnSuccess)
  {
    ApplyAudit();
    return base.SaveChanges(acceptAllChangesOnSuccess);
  }

  public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
  {
    ApplyAudit();
    return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
  }

  /// <summary>Tự gán CreatedAt/By, UpdatedAt/By (setter của entity là private nên đi qua ChangeTracker).</summary>
  private void ApplyAudit()
  {
    ChangeTracker.DetectChanges();
    var now = DateTimeOffset.UtcNow;
    var actor = currentUser.UserId ?? AuditConstants.SystemUserId;

    foreach (var entry in ChangeTracker.Entries<IAuditableEntity>())
    {
      switch (entry.State)
      {
        case EntityState.Added:
          entry.Property(nameof(IAuditableEntity.CreatedAt)).CurrentValue = now;
          // POST /auth/sync: user vừa tạo chưa có "người thực hiện" -> tự tạo chính nó
          entry.Property(nameof(IAuditableEntity.CreatedBy)).CurrentValue =
            actor == AuditConstants.SystemUserId && entry.Entity is User u ? u.Id : actor;
          break;
        case EntityState.Modified:
          entry.Property(nameof(IAuditableEntity.CreatedAt)).IsModified = false;
          entry.Property(nameof(IAuditableEntity.CreatedBy)).IsModified = false;
          entry.Property(nameof(IAuditableEntity.UpdatedAt)).CurrentValue = now;
          entry.Property(nameof(IAuditableEntity.UpdatedBy)).CurrentValue = actor;
          break;
      }
    }
  }
}
