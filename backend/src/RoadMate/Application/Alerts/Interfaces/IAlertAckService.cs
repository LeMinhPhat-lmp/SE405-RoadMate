using RoadMate.Application.Tracking;
using RoadMate.Application.Tracking.DTOs.Responses;
using RoadMate.Domain.Common;

namespace RoadMate.Application.Alerts.Interfaces;

public interface IAlertAckService
{
  Task<Result<AckResultResponse>> AckAsync(string token, CancellationToken ct);
}
