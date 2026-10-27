namespace RoadMate.Services.Interfaces;

public interface ITrackingService
{
  Task<Result> AddPointsAsync(Guid userId, Guid tripId, AddPointsRequest req, CancellationToken ct);
  Task<Result> HeartbeatAsync(Guid userId, Guid tripId, HeartbeatRequest req, CancellationToken ct);
  Task<Result<TrackingLinkDto>> CreateLinkAsync(Guid userId, Guid tripId, CancellationToken ct);
  Task<Result> DisableLinkAsync(Guid userId, Guid tripId, CancellationToken ct);
  Task<Result<PublicTrackDto>> GetPublicAsync(string token, CancellationToken ct);
  Task<Result<PublicRouteDto>> GetPublicRouteAsync(string token, CancellationToken ct);
  Task<Result> PublicAckAsync(string token, CancellationToken ct);
}
