using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace RoadMate.Data.Configurations;

public class TripConfiguration : IEntityTypeConfiguration<Trip>
{
  public void Configure(EntityTypeBuilder<Trip> e)
  {
    e.HasOne(x => x.Owner).WithMany(u => u.Trips).HasForeignKey(x => x.OwnerId);
    e.HasOne(x => x.Group).WithMany(g => g.Trips).HasForeignKey(x => x.GroupId).OnDelete(DeleteBehavior.SetNull);
    e.HasIndex(x => x.TrackingToken).IsUnique();
    e.HasIndex(x => new { x.OwnerId, x.Status });
  }
}
