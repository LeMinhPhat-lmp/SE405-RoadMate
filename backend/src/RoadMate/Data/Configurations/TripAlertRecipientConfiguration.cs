using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace RoadMate.Data.Configurations;

public class TripAlertRecipientConfiguration : IEntityTypeConfiguration<TripAlertRecipient>
{
  public void Configure(EntityTypeBuilder<TripAlertRecipient> e)
  {
    e.HasOne(x => x.Trip).WithMany(t => t.Recipients).HasForeignKey(x => x.TripId);
    e.HasOne(x => x.Contact).WithMany().HasForeignKey(x => x.ContactId);
    e.HasIndex(x => new { x.TripId, x.ContactId }).IsUnique();
  }
}
