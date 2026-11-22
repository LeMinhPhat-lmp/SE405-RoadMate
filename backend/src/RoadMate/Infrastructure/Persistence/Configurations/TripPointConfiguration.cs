using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RoadMate.Domain.Alerts;
using RoadMate.Domain.Groups;
using RoadMate.Domain.Trips;
using RoadMate.Domain.Users;

namespace RoadMate.Infrastructure.Persistence.Configurations;

/// <summary>Unique (TripId, RecordedAt): makes the bulk insert idempotent (ON CONFLICT DO NOTHING) when the app retries a batch.</summary>
public class TripPointConfiguration : IEntityTypeConfiguration<TripPoint>
{
  public void Configure(EntityTypeBuilder<TripPoint> e) => e.HasIndex(x => new { x.TripId, x.RecordedAt }).IsUnique();
}
