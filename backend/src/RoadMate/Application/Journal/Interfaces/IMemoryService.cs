using RoadMate.Application.Journal.DTOs.Requests;
using RoadMate.Application.Journal.DTOs.Responses;
using RoadMate.Domain.Common;

namespace RoadMate.Application.Journal.Interfaces;

public interface IMemoryService
{
  Task<Result<List<MemoryResponse>>> ListAsync(Guid userId, Guid tripId, CancellationToken ct);
  Task<Result<MemoryResponse>> AddAsync(Guid userId, Guid tripId, MemoryRequest req, CancellationToken ct);
  Task<Result<MemoryResponse>> UpdateAsync(Guid userId, Guid memoryId, MemoryRequest req, CancellationToken ct);
  Task<Result> DeleteAsync(Guid userId, Guid memoryId, CancellationToken ct);
}
