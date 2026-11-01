using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace RoadMate.Data.Configurations;

public class GroupConfiguration : IEntityTypeConfiguration<Group>
{
  public void Configure(EntityTypeBuilder<Group> e)
  {
    e.HasIndex(x => x.Code).IsUnique().HasFilter(SqlFilters.NotDeleted);
    e.HasOne(x => x.Leader).WithMany().HasForeignKey(x => x.LeaderId).OnDelete(DeleteBehavior.Restrict);
  }
}
