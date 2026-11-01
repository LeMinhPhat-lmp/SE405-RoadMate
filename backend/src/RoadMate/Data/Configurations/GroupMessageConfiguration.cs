using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace RoadMate.Data.Configurations;

public class GroupMessageConfiguration : IEntityTypeConfiguration<GroupMessage>
{
  public void Configure(EntityTypeBuilder<GroupMessage> e)
  {
    e.HasOne(x => x.Group).WithMany(g => g.Messages).HasForeignKey(x => x.GroupId);
    e.HasOne(x => x.Sender).WithMany().HasForeignKey(x => x.SenderId);
  }
}
