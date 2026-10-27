namespace RoadMate.Services.Interfaces;

public interface IJournalService
{
  Task<Result<JournalDto>> GetJournalAsync(Guid userId, Guid tripId, int tzOffsetHours, CancellationToken ct);
  Task<Result<List<MemoryDto>>> ListMemoriesAsync(Guid userId, Guid tripId, CancellationToken ct);
  Task<Result<MemoryDto>> AddMemoryAsync(Guid userId, Guid tripId, MemoryRequest req, CancellationToken ct);
  Task<Result<MemoryDto>> UpdateMemoryAsync(Guid userId, Guid memoryId, MemoryRequest req, CancellationToken ct);
  Task<Result> DeleteMemoryAsync(Guid userId, Guid memoryId, CancellationToken ct);
}
