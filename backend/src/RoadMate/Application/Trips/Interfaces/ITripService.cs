using RoadMate.Application.Trips.DTOs.Requests;
using RoadMate.Application.Trips.DTOs.Responses;
using RoadMate.Domain.Common;

namespace RoadMate.Application.Trips.Interfaces;

/// <summary>CRUD chuyến đi (bản nháp). Vòng đời chuyến (bắt đầu/kết thúc/nghỉ chân) nằm ở ITripLifecycleService.</summary>
public interface ITripService
{
  Task<Result<List<TripListItemResponse>>> ListAsync(Guid userId, string? status, CancellationToken ct);
  Task<Result<TripResponse>> GetAsync(Guid userId, Guid id, CancellationToken ct);
  Task<Result<TripResponse>> CreateAsync(Guid userId, TripUpsertRequest req, CancellationToken ct);
  Task<Result<TripResponse>> UpdateAsync(Guid userId, Guid id, TripUpsertRequest req, CancellationToken ct);
  Task<Result> DeleteAsync(Guid userId, Guid id, CancellationToken ct);
}
