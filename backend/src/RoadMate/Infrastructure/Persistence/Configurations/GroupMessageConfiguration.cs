using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RoadMate.Domain.Alerts;
using RoadMate.Domain.Groups;
using RoadMate.Domain.Trips;
using RoadMate.Domain.Users;

namespace RoadMate.Infrastructure.Persistence.Configurations;

public class GroupMessageConfiguration : IEntityTypeConfiguration<GroupMessage>
{
  public void Configure(EntityTypeBuilder<GroupMessage> e)
  {
    e.HasOne(x => x.Sender).WithMany().HasForeignKey(x => x.SenderId);
    e.HasIndex(x => new { x.GroupId, x.CreatedAt, x.Id });
  }
}
