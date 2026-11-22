using RoadMate.Application.Alerts;
using RoadMate.Application.Alerts.DTOs.Responses;
using RoadMate.Application.Tracking;
using RoadMate.Domain.Alerts;
using RoadMate.Domain.Users;

namespace RoadMate.Application.Abstractions.Persistence;

public interface IAlertReadModel
{
  Task<IReadOnlyList<AlertResponse>> ListForTripAsync(Guid tripId, CancellationToken ct = default);
}
