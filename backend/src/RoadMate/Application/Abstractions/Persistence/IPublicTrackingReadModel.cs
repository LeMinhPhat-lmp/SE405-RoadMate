using RoadMate.Application.Alerts;
using RoadMate.Application.Tracking;
using RoadMate.Application.Tracking.DTOs.Responses;
using RoadMate.Domain.Alerts;
using RoadMate.Domain.Users;

namespace RoadMate.Application.Abstractions.Persistence;

public interface IPublicTrackingReadModel
{
  /// <summary>Null when the token is unknown or expired at <paramref name="now"/>.</summary>
  Task<PublicTrackResponse?> GetAsync(string token, DateTimeOffset now, CancellationToken ct = default);
  Task<PublicRouteResponse?> GetRouteAsync(string token, DateTimeOffset now, int maxPoints, CancellationToken ct = default);
}
