using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace RoadMate.Data.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
  public void Configure(EntityTypeBuilder<User> e)
  {
    e.Property(x => x.FirebaseUid).HasMaxLength(128).IsRequired();
    e.HasIndex(x => x.FirebaseUid).IsUnique().HasFilter(SqlFilters.NotDeleted);
    e.HasIndex(x => x.Email).IsUnique().HasFilter(SqlFilters.NotDeleted);
    e.HasIndex(x => x.Phone);
  }
}
