using RoadMate.Application.Alerts.DTOs.Responses;
using RoadMate.Domain.Common;

namespace RoadMate.Application.Contacts.Interfaces;

public interface IContactTestService
{
  Task<Result<List<DeliveryResponse>>> TestAsync(Guid userId, Guid contactId, CancellationToken ct);
}
