using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RoadMate.Domain.Alerts;
using RoadMate.Domain.Groups;
using RoadMate.Domain.Trips;
using RoadMate.Domain.Users;

namespace RoadMate.Infrastructure.Persistence.Configurations;

public class TripConfiguration : IEntityTypeConfiguration<Trip>
{
  public void Configure(EntityTypeBuilder<Trip> e)
  {
    e.UseXmin();
    e.HasOne(x => x.Owner).WithMany().HasForeignKey(x => x.OwnerId);
    e.HasOne(x => x.Group).WithMany().HasForeignKey(x => x.GroupId).OnDelete(DeleteBehavior.SetNull);
    e.HasMany(x => x.Waypoints).WithOne().HasForeignKey(w => w.TripId).OnDelete(DeleteBehavior.Cascade);
    e.HasMany(x => x.Recipients).WithOne().HasForeignKey(r => r.TripId).OnDelete(DeleteBehavior.Cascade);
    e.HasOne(x => x.Live).WithOne().HasForeignKey<TripLive>(l => l.TripId).OnDelete(DeleteBehavior.Cascade);
    e.HasOne(x => x.Stats).WithOne().HasForeignKey<TripStats>(s => s.TripId).OnDelete(DeleteBehavior.Cascade);
    e.Navigation(x => x.Waypoints).UsePropertyAccessMode(PropertyAccessMode.Field);
    e.Navigation(x => x.Recipients).UsePropertyAccessMode(PropertyAccessMode.Field);
    e.Property(x => x.Title).HasMaxLength(200);
    e.HasIndex(x => x.TrackingToken).IsUnique();
    e.HasIndex(x => new { x.OwnerId, x.Status });
  }
}
