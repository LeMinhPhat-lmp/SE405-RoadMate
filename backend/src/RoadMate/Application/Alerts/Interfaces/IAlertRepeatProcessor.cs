using Microsoft.Extensions.Options;
using RoadMate.Application.Abstractions.Messaging;
using RoadMate.Application.Abstractions.Persistence;
using RoadMate.Domain.Alerts;
using RoadMate.Domain.Trips;

namespace RoadMate.Application.Alerts.Interfaces;

public interface IAlertRepeatProcessor
{
  Task<int> ProcessDueAsync(string workerId, CancellationToken ct);
}
