using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace RoadMate.Data.Configurations;

public class EmergencyContactConfiguration : IEntityTypeConfiguration<EmergencyContact>
{
  public void Configure(EntityTypeBuilder<EmergencyContact> e)
  {
    e.Property(x => x.Phone).HasMaxLength(20);
    e.HasOne(x => x.User).WithMany(u => u.EmergencyContacts).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
    e.HasOne(x => x.LinkedUser).WithMany().HasForeignKey(x => x.LinkedUserId).OnDelete(DeleteBehavior.SetNull);
    e.HasIndex(x => x.Phone);
  }
}
