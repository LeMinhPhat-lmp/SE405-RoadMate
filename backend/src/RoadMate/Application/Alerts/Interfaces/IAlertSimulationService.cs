using RoadMate.Application.Alerts.DTOs.Requests;
using RoadMate.Application.Alerts.DTOs.Responses;
using RoadMate.Domain.Common;

namespace RoadMate.Application.Alerts.Interfaces;

public interface IAlertSimulationService
{
  Task<Result<AlertResponse>> SimulateAsync(Guid userId, SimulateAlertRequest req, CancellationToken ct);
}
