namespace RoadMate.Services.Journal;

public interface IMemoryService
{
  Task<Result<List<MemoryDto>>> ListAsync(Guid userId, Guid tripId, CancellationToken ct);
  Task<Result<MemoryDto>> AddAsync(Guid userId, Guid tripId, MemoryRequest req, CancellationToken ct);
  Task<Result<MemoryDto>> UpdateAsync(Guid userId, Guid memoryId, MemoryRequest req, CancellationToken ct);
  Task<Result> DeleteAsync(Guid userId, Guid memoryId, CancellationToken ct);
}
