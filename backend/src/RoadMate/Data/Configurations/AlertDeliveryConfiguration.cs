using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace RoadMate.Data.Configurations;

public class AlertDeliveryConfiguration : IEntityTypeConfiguration<AlertDelivery>
{
  public void Configure(EntityTypeBuilder<AlertDelivery> e)
  {
    e.HasOne(x => x.Alert).WithMany(a => a.Deliveries).HasForeignKey(x => x.AlertId);
    e.HasOne(x => x.Contact).WithMany().HasForeignKey(x => x.ContactId).OnDelete(DeleteBehavior.SetNull);
  }
}
