using RoadMate.Domain.Common;
using RoadMate.Application.Maps.DTOs.Responses;
using RoadMate.Application.Maps.DTOs.Requests;

namespace RoadMate.Application.Trips.Interfaces;

/// <summary>Tính lộ trình của chuyến bằng Goong và lưu lại phía server (nguồn tin cậy cho phát hiện lệch tuyến).</summary>
public interface ITripRouteService
{
  Task<Result<TripRouteResponse>> RefreshAsync(Guid userId, Guid tripId, RefreshRouteRequest req, CancellationToken ct);
}
