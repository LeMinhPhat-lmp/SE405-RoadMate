using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace RoadMate.Data.Configurations;

public class GroupMemberConfiguration : IEntityTypeConfiguration<GroupMember>
{
  public void Configure(EntityTypeBuilder<GroupMember> e)
  {
    e.HasOne(x => x.Group).WithMany(g => g.Members).HasForeignKey(x => x.GroupId);
    e.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId);
    e.HasIndex(x => new { x.GroupId, x.UserId }).IsUnique();
  }
}
