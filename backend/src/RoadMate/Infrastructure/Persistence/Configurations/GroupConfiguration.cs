using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RoadMate.Domain.Alerts;
using RoadMate.Domain.Groups;
using RoadMate.Domain.Trips;
using RoadMate.Domain.Users;

namespace RoadMate.Infrastructure.Persistence.Configurations;

public class GroupConfiguration : IEntityTypeConfiguration<Group>
{
  public void Configure(EntityTypeBuilder<Group> e)
  {
    e.UseXmin();
    e.HasOne(x => x.Leader).WithMany().HasForeignKey(x => x.LeaderId).OnDelete(DeleteBehavior.Restrict);
    e.HasMany(x => x.Members).WithOne().HasForeignKey(m => m.GroupId).OnDelete(DeleteBehavior.Cascade);
    e.Navigation(x => x.Members).UsePropertyAccessMode(PropertyAccessMode.Field);
    e.Property(x => x.Code).HasMaxLength(6).IsRequired();
    e.HasIndex(x => x.Code).IsUnique().HasFilter(SqlFilters.NotDeleted);
  }
}
