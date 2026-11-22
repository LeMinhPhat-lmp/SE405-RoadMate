using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RoadMate.Domain.Alerts;
using RoadMate.Domain.Groups;
using RoadMate.Domain.Trips;
using RoadMate.Domain.Users;

namespace RoadMate.Infrastructure.Persistence.Configurations;

public class EmergencyContactConfiguration : IEntityTypeConfiguration<EmergencyContact>
{
  public void Configure(EntityTypeBuilder<EmergencyContact> e)
  {
    e.Property(x => x.Name).HasMaxLength(200).IsRequired();
    e.Property(x => x.Phone).HasMaxLength(20).IsRequired();
    e.HasOne(x => x.LinkedUser).WithMany().HasForeignKey(x => x.LinkedUserId).OnDelete(DeleteBehavior.SetNull);
    e.HasIndex(x => x.Phone);
  }
}
