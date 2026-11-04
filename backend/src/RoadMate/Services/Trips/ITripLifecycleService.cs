namespace RoadMate.Services.Trips;

/// <summary>Vòng đời chuyến: Draft -> Active -> Finished, cùng chế độ Nghỉ chân khi đang đi.</summary>
public interface ITripLifecycleService
{
  Task<Result<StartTripResponse>> StartAsync(Guid userId, Guid id, StartTripRequest req, CancellationToken ct);
  Task<Result<TripSummaryDto>> FinishAsync(Guid userId, Guid id, CancellationToken ct);
  Task<Result<TripSummaryDto>> SummaryAsync(Guid userId, Guid id, CancellationToken ct);
  Task<Result<TripDto>> SetRestAsync(Guid userId, Guid id, int minutes, CancellationToken ct);
  Task<Result<TripDto>> ClearRestAsync(Guid userId, Guid id, CancellationToken ct);
  /// <summary>Chủ chuyến báo "vẫn đang đi": dời giờ dự kiến về, reset nhắc về trễ và đóng cảnh báo lateReturn đang mở.</summary>
  Task<Result<TripDto>> ExtendReturnAsync(Guid userId, Guid id, DateTime expectedReturnTime, CancellationToken ct);
}
