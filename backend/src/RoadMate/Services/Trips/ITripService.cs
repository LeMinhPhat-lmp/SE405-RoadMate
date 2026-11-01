namespace RoadMate.Services.Trips;

/// <summary>CRUD chuyến đi (bản nháp). Vòng đời chuyến (bắt đầu/kết thúc/nghỉ chân) nằm ở ITripLifecycleService.</summary>
public interface ITripService
{
  Task<Result<List<TripListItemDto>>> ListAsync(Guid userId, string? status, CancellationToken ct);
  Task<Result<TripDto>> GetAsync(Guid userId, Guid id, CancellationToken ct);
  Task<Result<TripDto>> CreateAsync(Guid userId, TripUpsertRequest req, CancellationToken ct);
  Task<Result<TripDto>> UpdateAsync(Guid userId, Guid id, TripUpsertRequest req, CancellationToken ct);
  Task<Result> DeleteAsync(Guid userId, Guid id, CancellationToken ct);
}
