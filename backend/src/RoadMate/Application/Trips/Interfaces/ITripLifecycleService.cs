using RoadMate.Application.Trips.DTOs.Requests;
using RoadMate.Application.Trips.DTOs.Responses;
using RoadMate.Domain.Common;

namespace RoadMate.Application.Trips.Interfaces;

/// <summary>Vòng đời chuyến: Draft -> Active -> Finished, cùng chế độ Nghỉ chân khi đang đi.</summary>
public interface ITripLifecycleService
{
  Task<Result<StartTripResponse>> StartAsync(Guid userId, Guid id, StartTripRequest req, CancellationToken ct);
  Task<Result<TripSummaryResponse>> FinishAsync(Guid userId, Guid id, CancellationToken ct);
  Task<Result<TripSummaryResponse>> SummaryAsync(Guid userId, Guid id, CancellationToken ct);
  Task<Result<TripResponse>> SetRestAsync(Guid userId, Guid id, int minutes, CancellationToken ct);
  Task<Result<TripResponse>> ClearRestAsync(Guid userId, Guid id, CancellationToken ct);
  /// <summary>Chủ chuyến báo "vẫn đang đi": dời giờ dự kiến về, reset nhắc về trễ và đóng cảnh báo lateReturn đang mở.</summary>
  Task<Result<TripResponse>> ExtendReturnAsync(Guid userId, Guid id, DateTime expectedReturnTime, CancellationToken ct);
}
