using RoadMate.Application.Alerts.DTOs.Requests;
using RoadMate.Application.Alerts.DTOs.Responses;
using RoadMate.Domain.Common;

namespace RoadMate.Application.Alerts.Interfaces;

public interface IAlertService
{
  /// <summary>Same <c>clientId</c> + same user returns the existing alert (no new notifications).</summary>
  Task<Result<AlertResponse>> CreateAsync(Guid userId, Guid tripId, CreateAlertRequest req, CancellationToken ct);
  Task<Result<List<AlertResponse>>> ListAsync(Guid userId, Guid tripId, CancellationToken ct);
}