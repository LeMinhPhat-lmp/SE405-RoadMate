using RoadMate.Domain.Abstractions;
using RoadMate.Domain.Alerts;
using RoadMate.Domain.Common;

namespace RoadMate.Application.Abstractions.Messaging;

/// <summary>Request-shape validation (required fields, lengths, ranges). Invariants live in the domain.</summary>
public interface IRequestValidator<in T>
{
  Result Validate(T request, DateTimeOffset now);
}
