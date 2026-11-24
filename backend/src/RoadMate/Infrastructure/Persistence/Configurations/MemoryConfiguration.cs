using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RoadMate.Domain.Alerts;
using RoadMate.Domain.Groups;
using RoadMate.Domain.Trips;
using RoadMate.Domain.Users;

namespace RoadMate.Infrastructure.Persistence.Configurations;

public class MemoryConfiguration : IEntityTypeConfiguration<Memory>
{
  public void Configure(EntityTypeBuilder<Memory> e)
  {
    e.HasOne(x => x.Trip).WithMany().HasForeignKey(x => x.TripId);
    e.HasIndex(x => new { x.TripId, x.TakenAt, x.Id });
  }
}
