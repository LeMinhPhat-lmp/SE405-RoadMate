using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace RoadMate.Data.Configurations;

public class TripStatsConfiguration : IEntityTypeConfiguration<TripStats>
{
  public void Configure(EntityTypeBuilder<TripStats> e) =>
    e.HasOne(x => x.Trip).WithOne(t => t.Stats).HasForeignKey<TripStats>(x => x.TripId);
}
