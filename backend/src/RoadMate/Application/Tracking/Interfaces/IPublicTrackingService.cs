using RoadMate.Application.Tracking.DTOs.Responses;
using RoadMate.Domain.Common;

namespace RoadMate.Application.Tracking.Interfaces;

/// <summary>Phía người thân: truy cập bằng trackingToken, không cần đăng nhập. Xác nhận "đã liên lạc" nằm ở IAlertAckService (token riêng từng liên hệ).</summary>
public interface IPublicTrackingService
{
  Task<Result<PublicTrackResponse>> GetAsync(string token, CancellationToken ct);
  Task<Result<PublicRouteResponse>> GetRouteAsync(string token, CancellationToken ct);
}
