namespace RoadMate.Services.Interfaces;

public interface ITripService
{
  Task<Result<List<TripListItemDto>>> ListAsync(Guid userId, string? status, CancellationToken ct);
  Task<Result<TripDto>> GetAsync(Guid userId, Guid id, CancellationToken ct);
  Task<Result<TripDto>> CreateAsync(Guid userId, TripUpsertRequest req, CancellationToken ct);
  Task<Result<TripDto>> UpdateAsync(Guid userId, Guid id, TripUpsertRequest req, CancellationToken ct);
  Task<Result> DeleteAsync(Guid userId, Guid id, CancellationToken ct);
  Task<Result<StartTripResponse>> StartAsync(Guid userId, Guid id, StartTripRequest req, CancellationToken ct);
  Task<Result<TripSummaryDto>> FinishAsync(Guid userId, Guid id, CancellationToken ct);
  Task<Result<TripSummaryDto>> SummaryAsync(Guid userId, Guid id, CancellationToken ct);
  Task<Result<TripDto>> SetRestAsync(Guid userId, Guid id, int minutes, CancellationToken ct);
  Task<Result<TripDto>> ClearRestAsync(Guid userId, Guid id, CancellationToken ct);
}
