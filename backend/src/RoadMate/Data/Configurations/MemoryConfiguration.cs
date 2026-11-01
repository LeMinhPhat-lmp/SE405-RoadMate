using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace RoadMate.Data.Configurations;

public class MemoryConfiguration : IEntityTypeConfiguration<Memory>
{
  public void Configure(EntityTypeBuilder<Memory> e)
  {
    e.HasOne(x => x.Trip).WithMany(t => t.Memories).HasForeignKey(x => x.TripId);
    e.HasIndex(x => new { x.TripId, x.TakenAt });
  }
}
