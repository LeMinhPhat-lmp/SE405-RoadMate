using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RoadMate.Domain.Alerts;
using RoadMate.Domain.Groups;
using RoadMate.Domain.Trips;
using RoadMate.Domain.Users;

namespace RoadMate.Infrastructure.Persistence.Configurations;

/// <summary>xmin (PostgreSQL system column) as optimistic-concurrency token: two writers changing the same aggregate row cannot both win.</summary>
internal static class ConcurrencyExtensions
{
  public static void UseXmin<T>(this EntityTypeBuilder<T> e) where T : class =>
    e.Property<uint>("xmin").HasColumnName("xmin").HasColumnType("xid").ValueGeneratedOnAddOrUpdate().IsConcurrencyToken();
}
