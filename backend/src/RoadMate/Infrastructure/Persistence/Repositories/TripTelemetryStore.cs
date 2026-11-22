using Microsoft.EntityFrameworkCore;
using Npgsql;
using NpgsqlTypes;
using RoadMate.Application.Abstractions.Persistence;
using RoadMate.Domain.Trips;

namespace RoadMate.Infrastructure.Persistence.Repositories;

/// <summary>Set-based writes for the hot path (no aggregate, no change tracker).</summary>
public class TripTelemetryStore(AppDbContext db) : ITripTelemetryWriter, ITripPointReader
{
  public async Task<int> AppendPointsAsync(Guid tripId, IReadOnlyList<TripPointSample> s, CancellationToken ct = default)
  {
    if (s.Count == 0) return 0;
    // One statement for the whole batch; ON CONFLICT makes an app retry of the same batch harmless.
    const string sql = """
      INSERT INTO "TripPoints" ("Id","TripId","Lat","Lng","Speed","Altitude","Battery","RecordedAt")
      SELECT gen_random_uuid(), @trip, t.lat, t.lng, t.speed, t.alt, t.bat, t.rec
      FROM unnest(@lat, @lng, @speed, @alt, @bat, @rec) AS t(lat, lng, speed, alt, bat, rec)
      ON CONFLICT ("TripId","RecordedAt") DO NOTHING
      """;
    return await db.Database.ExecuteSqlRawAsync(sql,
    [
      new NpgsqlParameter("trip", tripId),
      new NpgsqlParameter("lat", NpgsqlDbType.Array | NpgsqlDbType.Double) { Value = s.Select(p => p.Lat).ToArray() },
      new NpgsqlParameter("lng", NpgsqlDbType.Array | NpgsqlDbType.Double) { Value = s.Select(p => p.Lng).ToArray() },
      new NpgsqlParameter("speed", NpgsqlDbType.Array | NpgsqlDbType.Double) { Value = s.Select(p => p.Speed).ToArray() },
      new NpgsqlParameter("alt", NpgsqlDbType.Array | NpgsqlDbType.Double) { Value = s.Select(p => p.Altitude).ToArray() },
      new NpgsqlParameter("bat", NpgsqlDbType.Array | NpgsqlDbType.Integer) { Value = s.Select(p => p.Battery).ToArray() },
      new NpgsqlParameter("rec", NpgsqlDbType.Array | NpgsqlDbType.TimestampTz) { Value = s.Select(p => p.RecordedAt.UtcDateTime).ToArray() },
    ], ct);
  }

  /// <summary>
  /// Atomic "latest position wins": a single UPSERT guarded by <c>ReportedAt &lt;= @reported</c>. Same rule as <c>Trip.ApplyLivePosition</c>
  /// (equal timestamps are applied). When an older sample loses, only the receive clock (lost-signal timer) is refreshed.
  /// </summary>
  public async Task<bool> UpsertLiveIfNewerAsync(TripLiveUpdate u, CancellationToken ct = default)
  {
    const string upsert = """
      INSERT INTO "TripLives" ("Id","TripId","Lat","Lng","Speed","Battery","NetworkOk","ReportedAt","UpdatedAt")
      VALUES (gen_random_uuid(), @trip, @lat, @lng, @speed, @bat, @net, @reported, @received)
      ON CONFLICT ("TripId") DO UPDATE SET "Lat"=EXCLUDED."Lat","Lng"=EXCLUDED."Lng","Speed"=EXCLUDED."Speed",
        "Battery"=COALESCE(EXCLUDED."Battery","TripLives"."Battery"),"NetworkOk"=EXCLUDED."NetworkOk",
        "ReportedAt"=EXCLUDED."ReportedAt","UpdatedAt"=EXCLUDED."UpdatedAt"
      WHERE "TripLives"."ReportedAt" <= EXCLUDED."ReportedAt"
      """;
    var rows = await db.Database.ExecuteSqlRawAsync(upsert,
    [
      new NpgsqlParameter("trip", u.TripId), new NpgsqlParameter("lat", u.Location.Lat), new NpgsqlParameter("lng", u.Location.Lng),
      new NpgsqlParameter("speed", (object?)u.Speed ?? DBNull.Value), new NpgsqlParameter("bat", (object?)u.Battery ?? DBNull.Value),
      new NpgsqlParameter("net", u.NetworkOk), new NpgsqlParameter("reported", u.ReportedAt.UtcDateTime), new NpgsqlParameter("received", u.ReceivedAt.UtcDateTime),
    ], ct);
    if (rows > 0) return true;
    await db.Database.ExecuteSqlRawAsync("""UPDATE "TripLives" SET "UpdatedAt"=@received WHERE "TripId"=@trip""",
      [new NpgsqlParameter("received", u.ReceivedAt.UtcDateTime), new NpgsqlParameter("trip", u.TripId)], ct);
    return false;
  }

  public async Task<IReadOnlyList<TripPointSample>> ListAsync(Guid tripId, CancellationToken ct = default) =>
    await db.TripPoints.AsNoTracking().Where(p => p.TripId == tripId).OrderBy(p => p.RecordedAt)
      .Select(p => new TripPointSample(p.Lat, p.Lng, p.Speed, p.Altitude, p.Battery, p.RecordedAt)).ToListAsync(ct);

  public async Task<IReadOnlyList<TripPointSample>> LastAsync(Guid tripId, int take, CancellationToken ct = default) =>
    (await db.TripPoints.AsNoTracking().Where(p => p.TripId == tripId).OrderByDescending(p => p.RecordedAt).Take(take)
      .Select(p => new TripPointSample(p.Lat, p.Lng, p.Speed, p.Altitude, p.Battery, p.RecordedAt)).ToListAsync(ct)).AsEnumerable().Reverse().ToList();
}
