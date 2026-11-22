using RoadMate.Domain.Trips;
using RoadMate.Domain.ValueObjects;

namespace RoadMate.Application.Abstractions.Persistence;

/// <summary>Set-based telemetry writes (no aggregate load, no change tracker): GPS batch insert and the atomic "latest position wins" UPSERT.</summary>
public interface ITripTelemetryWriter
{
  /// <summary>Idempotent per (trip, recordedAt); returns the number of rows really inserted.</summary>
  Task<int> AppendPointsAsync(Guid tripId, IReadOnlyList<TripPointSample> samples, CancellationToken ct = default);
  /// <summary>Single UPSERT … WHERE ReportedAt &lt;= @reportedAt. True when the position advanced; false when an older sample was ignored (only UpdatedAt refreshed).</summary>
  Task<bool> UpsertLiveIfNewerAsync(TripLiveUpdate update, CancellationToken ct = default);
}
