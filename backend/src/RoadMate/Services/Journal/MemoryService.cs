namespace RoadMate.Services.Journal;

public class MemoryService(IUnitOfWork uow) : IMemoryService
{
  public async Task<Result<List<MemoryDto>>> ListAsync(Guid userId, Guid tripId, CancellationToken ct)
  {
    if (await uow.TripRepository.GetOwnedAsync(tripId, userId, ct) is null) return Errors.TripNotFound;
    return (await uow.MemoryRepository.ListByTripAsync(tripId, ct)).Select(m => m.ToDto()).ToList();
  }

  public async Task<Result<MemoryDto>> AddAsync(Guid userId, Guid tripId, MemoryRequest req, CancellationToken ct)
  {
    if (await uow.TripRepository.GetOwnedAsync(tripId, userId, ct) is null) return Errors.TripNotFound;
    var valid = MemoryRequestValidator.Validate(req);
    if (valid.IsFailure) return valid.Error;
    var m = new Memory { Id = Guid.NewGuid(), TripId = tripId };
    m.ApplyFrom(req);
    await uow.MemoryRepository.AddAsync(m, ct);
    await uow.SaveChangesAsync(ct);
    return m.ToDto();
  }

  public async Task<Result<MemoryDto>> UpdateAsync(Guid userId, Guid memoryId, MemoryRequest req, CancellationToken ct)
  {
    var m = await uow.MemoryRepository.GetOwnedAsync(memoryId, userId, ct);
    if (m is null) return Errors.MemoryNotFound;
    var valid = MemoryRequestValidator.Validate(req);
    if (valid.IsFailure) return valid.Error;
    m.ApplyFrom(req);
    await uow.SaveChangesAsync(ct);
    return m.ToDto();
  }

  public async Task<Result> DeleteAsync(Guid userId, Guid memoryId, CancellationToken ct)
  {
    var m = await uow.MemoryRepository.GetOwnedAsync(memoryId, userId, ct);
    if (m is null) return Errors.MemoryNotFound;
    uow.MemoryRepository.Delete(m);
    await uow.SaveChangesAsync(ct);
    return Result.Success();
  }
}
