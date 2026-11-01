using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace RoadMate.Data.Configurations;

public class TripLiveConfiguration : IEntityTypeConfiguration<TripLive>
{
  public void Configure(EntityTypeBuilder<TripLive> e) =>
    e.HasOne(x => x.Trip).WithOne(t => t.Live).HasForeignKey<TripLive>(x => x.TripId);
}
