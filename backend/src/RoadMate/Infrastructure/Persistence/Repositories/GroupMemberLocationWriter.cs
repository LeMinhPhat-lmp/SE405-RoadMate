using Microsoft.EntityFrameworkCore;
using Npgsql;
using RoadMate.Application.Abstractions.Persistence;
using RoadMate.Domain.ValueObjects;

namespace RoadMate.Infrastructure.Persistence.Repositories;

public class GroupMemberLocationWriter(AppDbContext db) : IGroupMemberLocationWriter
{
  public async Task UpdateAsync(Guid groupId, Guid userId, GeoPoint at, int? battery, DateTimeOffset now, CancellationToken ct = default) =>
    await db.Database.ExecuteSqlRawAsync("""
      UPDATE "GroupMembers" SET "LastLat"=@lat,"LastLng"=@lng,"LastBattery"=@bat,"LocationUpdatedAt"=@now WHERE "GroupId"=@g AND "UserId"=@u
      """,
    [
      new NpgsqlParameter("lat", at.Lat), new NpgsqlParameter("lng", at.Lng), new NpgsqlParameter("bat", (object?)battery ?? DBNull.Value),
      new NpgsqlParameter("now", now.UtcDateTime), new NpgsqlParameter("g", groupId), new NpgsqlParameter("u", userId),
    ], ct);
}
