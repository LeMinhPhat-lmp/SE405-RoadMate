using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace RoadMate.Data;

public static class ChangeTrackerAuditExtensions
{
  /// <summary>
  /// Tự gán CreatedAt/By, UpdatedAt/By (setter của entity là private nên đi qua ChangeTracker).
  /// Gọi ngay trước SaveChanges.
  /// </summary>
  public static void ApplyAudit(this ChangeTracker tracker, Guid actor, DateTimeOffset now)
  {
    tracker.DetectChanges();
    foreach (var entry in tracker.Entries<IAuditableEntity>())
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
