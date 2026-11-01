using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace RoadMate.Data.Configurations;

public class TripPointConfiguration : IEntityTypeConfiguration<TripPoint>
{
  public void Configure(EntityTypeBuilder<TripPoint> e)
  {
    e.HasOne(x => x.Trip).WithMany(t => t.Points).HasForeignKey(x => x.TripId);
    e.HasIndex(x => new { x.TripId, x.RecordedAt });
  }
}
