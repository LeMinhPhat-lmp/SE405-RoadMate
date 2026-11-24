using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RoadMate.Domain.Alerts;
using RoadMate.Domain.Groups;
using RoadMate.Domain.Trips;
using RoadMate.Domain.Users;

namespace RoadMate.Infrastructure.Persistence.Configurations;

public class TripLiveConfiguration : IEntityTypeConfiguration<TripLive>
{
  public void Configure(EntityTypeBuilder<TripLive> e) => e.HasIndex(x => x.TripId).IsUnique();
}
