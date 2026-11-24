using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RoadMate.Domain.Alerts;
using RoadMate.Domain.Groups;
using RoadMate.Domain.Trips;
using RoadMate.Domain.Users;

namespace RoadMate.Infrastructure.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
  public void Configure(EntityTypeBuilder<User> e)
  {
    e.UseXmin();
    e.Property(x => x.FirebaseUid).HasMaxLength(128).IsRequired();
    e.HasIndex(x => x.FirebaseUid).IsUnique().HasFilter(SqlFilters.NotDeleted);
    e.HasIndex(x => x.Email).IsUnique().HasFilter(SqlFilters.NotDeleted);
    e.HasIndex(x => x.Phone);
    e.HasMany(x => x.Contacts).WithOne().HasForeignKey(c => c.UserId);
    e.Navigation(x => x.Contacts).UsePropertyAccessMode(PropertyAccessMode.Field);
  }
}
