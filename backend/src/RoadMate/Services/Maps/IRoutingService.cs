namespace RoadMate.Services.Maps;

public interface IRoutingService
{
  Task<Result<DirectionsResponse>> DirectionsAsync(DirectionsRequest req, CancellationToken ct);
}
