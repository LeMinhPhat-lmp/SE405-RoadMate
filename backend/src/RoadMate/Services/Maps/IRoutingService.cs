namespace RoadMate.Services.Maps;

public interface IRoutingService
{
  /// <summary>Lộ trình origin → (waypoints) → destination; mỗi chặng 1 request Directions V2 rồi nối lại.</summary>
  Task<Result<DirectionsResponse>> DirectionsAsync(DirectionsRequest req, CancellationToken ct);
  /// <summary>Khoảng cách/thời gian giữa nhiều cặp điểm (Distance Matrix V2).</summary>
  Task<Result<DistanceMatrixResponse>> MatrixAsync(DistanceMatrixRequest req, CancellationToken ct);
}
