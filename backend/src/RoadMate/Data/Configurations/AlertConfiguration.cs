using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace RoadMate.Data.Configurations;

public class AlertConfiguration : IEntityTypeConfiguration<Alert>
{
  public void Configure(EntityTypeBuilder<Alert> e)
  {
    e.HasOne(x => x.Trip).WithMany(t => t.Alerts).HasForeignKey(x => x.TripId);
    e.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId);
    e.HasIndex(x => x.ClientId).IsUnique();
    e.HasIndex(x => new { x.Status, x.NextRepeatAt });
  }
}
