using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RoadMate.Domain.Alerts;
using RoadMate.Domain.Groups;
using RoadMate.Domain.Trips;
using RoadMate.Domain.Users;

namespace RoadMate.Infrastructure.Persistence.Configurations;

public class TripWaypointConfiguration : IEntityTypeConfiguration<TripWaypoint>
{
  public void Configure(EntityTypeBuilder<TripWaypoint> e) => e.HasIndex(x => new { x.TripId, x.Seq });
}
