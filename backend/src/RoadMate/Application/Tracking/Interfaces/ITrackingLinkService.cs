using RoadMate.Application.Tracking.DTOs.Responses;
using RoadMate.Domain.Common;

namespace RoadMate.Application.Tracking.Interfaces;

public interface ITrackingLinkService
{
  Task<Result<TrackingLinkResponse>> CreateLinkAsync(Guid userId, Guid tripId, CancellationToken ct);
  Task<Result> DisableLinkAsync(Guid userId, Guid tripId, CancellationToken ct);
}
