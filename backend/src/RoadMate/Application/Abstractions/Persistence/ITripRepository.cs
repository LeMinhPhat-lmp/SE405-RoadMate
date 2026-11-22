using RoadMate.Domain.Trips;
using RoadMate.Domain.ValueObjects;

namespace RoadMate.Application.Abstractions.Persistence;

public interface ITripRepository : IRepository<Trip>, ISoftDeleteRepository<Trip>
{
  /// <summary>Tracked trip of this owner (no children loaded); null when missing or owned by someone else.</summary>
  Task<Trip?> GetOwnedAsync(Guid tripId, Guid ownerId, CancellationToken ct = default);
  /// <summary>Tracked, with waypoints, recipients, live position and stats.</summary>
  Task<Trip?> GetOwnedWithDetailsAsync(Guid tripId, Guid ownerId, CancellationToken ct = default);
  /// <summary>No tracking, with waypoints and recipients; newest first.</summary>
  Task<IReadOnlyList<Trip>> ListByOwnerAsync(Guid ownerId, TripStatus? status, CancellationToken ct = default);
  Task<bool> HasActiveAsync(Guid ownerId, CancellationToken ct = default);
  /// <summary>Tracked active trips with Live and Waypoints (monitors).</summary>
  Task<IReadOnlyList<Trip>> ListActiveForMonitoringAsync(CancellationToken ct = default);
  /// <summary>Tracked trips of <paramref name="userId"/> attached to <paramref name="groupId"/>.</summary>
  Task<IReadOnlyList<Trip>> ListInGroupAsync(Guid userId, Guid groupId, CancellationToken ct = default);
}
