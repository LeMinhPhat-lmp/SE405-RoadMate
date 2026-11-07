using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace RoadMate.Data.Configurations;

public class AlertAckConfiguration : IEntityTypeConfiguration<AlertAck>
{
  public void Configure(EntityTypeBuilder<AlertAck> e)
  {
    e.Property(x => x.Token).HasMaxLength(64).IsRequired();
    e.HasOne(x => x.Alert).WithMany(a => a.Acks).HasForeignKey(x => x.AlertId);
    e.HasOne(x => x.Contact).WithMany().HasForeignKey(x => x.ContactId);
    e.HasIndex(x => x.Token).IsUnique();
    e.HasIndex(x => new { x.AlertId, x.ContactId }).IsUnique();
  }
}
