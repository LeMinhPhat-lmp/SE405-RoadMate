using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RoadMate.Domain.Alerts;
using RoadMate.Domain.Groups;
using RoadMate.Domain.Trips;
using RoadMate.Domain.Users;

namespace RoadMate.Infrastructure.Persistence.Configurations;

public class TripAlertRecipientConfiguration : IEntityTypeConfiguration<TripAlertRecipient>
{
  public void Configure(EntityTypeBuilder<TripAlertRecipient> e)
  {
    e.HasOne(x => x.Contact).WithMany().HasForeignKey(x => x.ContactId).OnDelete(DeleteBehavior.Cascade);
    e.HasIndex(x => new { x.TripId, x.ContactId }).IsUnique();
  }
}
