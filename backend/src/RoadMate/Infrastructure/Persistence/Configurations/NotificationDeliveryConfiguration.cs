using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RoadMate.Domain.Alerts;
using RoadMate.Domain.Groups;
using RoadMate.Domain.Trips;
using RoadMate.Domain.Users;

namespace RoadMate.Infrastructure.Persistence.Configurations;

/// <summary>Outbox table. The unique key makes planning idempotent; the (Status, NextAttemptAt) index serves the claim query.</summary>
public class NotificationDeliveryConfiguration : IEntityTypeConfiguration<NotificationDelivery>
{
  public void Configure(EntityTypeBuilder<NotificationDelivery> e)
  {
    e.HasOne(x => x.Contact).WithMany().HasForeignKey(x => x.ContactId).OnDelete(DeleteBehavior.SetNull);
    e.Property(x => x.Error).HasMaxLength(500);
    e.Property(x => x.LockedBy).HasMaxLength(100);
    e.Property(x => x.ProviderMessageId).HasMaxLength(200);
    e.HasIndex(x => new { x.AlertId, x.ContactId, x.Channel, x.Kind, x.Sequence }).IsUnique();
    e.HasIndex(x => new { x.Status, x.NextAttemptAt });
  }
}
