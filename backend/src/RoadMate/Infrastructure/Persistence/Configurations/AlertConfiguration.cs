using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RoadMate.Domain.Alerts;
using RoadMate.Domain.Groups;
using RoadMate.Domain.Trips;
using RoadMate.Domain.Users;

namespace RoadMate.Infrastructure.Persistence.Configurations;

public class AlertConfiguration : IEntityTypeConfiguration<Alert>
{
  public void Configure(EntityTypeBuilder<Alert> e)
  {
    e.HasOne(x => x.Trip).WithMany().HasForeignKey(x => x.TripId);
    e.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId);
    e.HasMany(x => x.Deliveries).WithOne(d => d.Alert).HasForeignKey(d => d.AlertId).OnDelete(DeleteBehavior.Cascade);
    e.HasMany(x => x.Acks).WithOne(a => a.Alert).HasForeignKey(a => a.AlertId).OnDelete(DeleteBehavior.Cascade);
    e.Navigation(x => x.Deliveries).UsePropertyAccessMode(PropertyAccessMode.Field);
    e.Navigation(x => x.Acks).UsePropertyAccessMode(PropertyAccessMode.Field);
    e.Property(x => x.ClientId).HasMaxLength(100).IsRequired();
    e.Property(x => x.LockedBy).HasMaxLength(100);
    e.HasIndex(x => x.ClientId).IsUnique();
    e.HasIndex(x => new { x.TripId, x.CreatedAt, x.Id });
    e.HasIndex(x => new { x.Status, x.NextRepeatAt });
  }
}
