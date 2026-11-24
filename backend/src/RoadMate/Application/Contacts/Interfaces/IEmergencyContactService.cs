using RoadMate.Application.Contacts.DTOs.Requests;
using RoadMate.Application.Contacts.DTOs.Responses;
using RoadMate.Domain.Common;

namespace RoadMate.Application.Contacts.Interfaces;

public interface IEmergencyContactService
{
  Task<Result<List<ContactResponse>>> ListAsync(Guid userId, CancellationToken ct);
  Task<Result<ContactResponse>> CreateAsync(Guid userId, ContactRequest req, CancellationToken ct);
  Task<Result<ContactResponse>> UpdateAsync(Guid userId, Guid id, ContactRequest req, CancellationToken ct);
  Task<Result> DeleteAsync(Guid userId, Guid id, CancellationToken ct);
}
