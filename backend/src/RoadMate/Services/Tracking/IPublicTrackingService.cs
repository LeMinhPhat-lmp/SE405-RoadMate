namespace RoadMate.Services.Tracking;

/// <summary>Phía người thân: truy cập bằng trackingToken, không cần đăng nhập.</summary>
public interface IPublicTrackingService
{
  Task<Result<PublicTrackDto>> GetAsync(string token, CancellationToken ct);
  Task<Result<PublicRouteDto>> GetRouteAsync(string token, CancellationToken ct);
  Task<Result> AckAsync(string token, CancellationToken ct);
}
