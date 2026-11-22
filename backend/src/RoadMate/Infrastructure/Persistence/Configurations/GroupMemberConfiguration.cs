using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RoadMate.Domain.Alerts;
using RoadMate.Domain.Groups;
using RoadMate.Domain.Trips;
using RoadMate.Domain.Users;

namespace RoadMate.Infrastructure.Persistence.Configurations;

public class GroupMemberConfiguration : IEntityTypeConfiguration<GroupMember>
{
  public void Configure(EntityTypeBuilder<GroupMember> e)
  {
    e.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId);
    e.HasIndex(x => new { x.GroupId, x.UserId }).IsUnique();
  }
}
