namespace RoadMate.Services.Tracking;

public interface ITrackingLinkService
{
  Task<Result<TrackingLinkDto>> CreateLinkAsync(Guid userId, Guid tripId, CancellationToken ct);
  Task<Result> DisableLinkAsync(Guid userId, Guid tripId, CancellationToken ct);
}
