using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RoadMate.Domain.Alerts;
using RoadMate.Domain.Groups;
using RoadMate.Domain.Trips;
using RoadMate.Domain.Users;

namespace RoadMate.Infrastructure.Persistence.Configurations;

public class AlertAckConfiguration : IEntityTypeConfiguration<AlertAck>
{
  public void Configure(EntityTypeBuilder<AlertAck> e)
  {
    e.HasOne(x => x.Contact).WithMany().HasForeignKey(x => x.ContactId);
    e.Property(x => x.Token).HasMaxLength(64).IsRequired();
    e.HasIndex(x => x.Token).IsUnique();
    e.HasIndex(x => new { x.AlertId, x.ContactId }).IsUnique();
  }
}
