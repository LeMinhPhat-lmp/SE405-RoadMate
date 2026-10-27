namespace RoadMate.Services.Interfaces;

public interface IEmergencyContactService
{
  Task<Result<List<ContactDto>>> ListAsync(Guid userId, CancellationToken ct);
  Task<Result<ContactDto>> CreateAsync(Guid userId, ContactRequest req, CancellationToken ct);
  Task<Result<ContactDto>> UpdateAsync(Guid userId, Guid id, ContactRequest req, CancellationToken ct);
  Task<Result> DeleteAsync(Guid userId, Guid id, CancellationToken ct);
  Task<Result<List<DeliveryDto>>> TestAsync(Guid userId, Guid id, CancellationToken ct);
}
