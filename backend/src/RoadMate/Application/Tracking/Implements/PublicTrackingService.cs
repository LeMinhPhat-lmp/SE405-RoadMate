using RoadMate.Application.Abstractions.Persistence;
using RoadMate.Application.Tracking.DTOs.Responses;
using RoadMate.Application.Tracking.Interfaces;
using RoadMate.Domain.Common;

namespace RoadMate.Application.Tracking.Implements;

public class PublicTrackingService(IPublicTrackingReadModel read, TimeProvider clock) : IPublicTrackingService
{
  public const int RoutePointLimit = 2000;

  public async Task<Result<PublicTrackResponse>> GetAsync(string token, CancellationToken ct) =>
    await read.GetAsync(token, clock.GetUtcNow(), ct) ?? (Result<PublicTrackResponse>)Errors.LinkInvalid;

  public async Task<Result<PublicRouteResponse>> GetRouteAsync(string token, CancellationToken ct) =>
    await read.GetRouteAsync(token, clock.GetUtcNow(), RoutePointLimit, ct) ?? (Result<PublicRouteResponse>)Errors.LinkInvalid;
}
