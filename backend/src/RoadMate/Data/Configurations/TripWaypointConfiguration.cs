using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace RoadMate.Data.Configurations;

public class TripWaypointConfiguration : IEntityTypeConfiguration<TripWaypoint>
{
  public void Configure(EntityTypeBuilder<TripWaypoint> e) =>
    e.HasOne(x => x.Trip).WithMany(t => t.Waypoints).HasForeignKey(x => x.TripId);
}
