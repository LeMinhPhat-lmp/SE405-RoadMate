using Microsoft.Extensions.Options;
using RoadMate.Application.Abstractions.Messaging;
using RoadMate.Application.Abstractions.Persistence;
using RoadMate.Domain.Alerts;

namespace RoadMate.Application.Alerts.Interfaces;

public interface IOutboxProcessor
{
  /// <summary>Claims due outbox rows (lease), sends them, records the outcome. Returns how many rows were handled.</summary>
  Task<int> ProcessBatchAsync(string workerId, CancellationToken ct);
}
