using RoadMate.Application.Alerts.DTOs.Responses;
using RoadMate.Application.Common;

namespace RoadMate.Application.Abstractions.Persistence;

public interface IAlertReadModel
{
  Task<IReadOnlyList<AlertResponse>> ListForTripAsync(Guid tripId, CancellationToken ct = default);
  /// <summary>No tracking, keyset-paginated by descending (CreatedAt, Id).</summary>
  Task<CursorPage<AlertResponse>> ListForTripPageAsync(Guid tripId, CursorPosition? after, int limit, CancellationToken ct = default);
}
