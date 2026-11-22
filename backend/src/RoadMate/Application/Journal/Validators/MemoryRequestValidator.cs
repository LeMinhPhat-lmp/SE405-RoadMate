using RoadMate.Application.Abstractions.Messaging;
using RoadMate.Application.Journal.DTOs.Requests;
using RoadMate.Domain.Common;
using RoadMate.Domain.Trips;

namespace RoadMate.Application.Journal.Validators;

/// <summary>Only the request-shape rule (type parsing) stays here; note/media/rating/location invariants moved into <c>Memory</c>.</summary>
public sealed class MemoryRequestValidator : IRequestValidator<MemoryRequest>
{
  public Result Validate(MemoryRequest r, DateTimeOffset now) =>
    EnumWire.TryParse<MemoryType>(r.Type, out _) ? Result.Success() : Error.Validation("INVALID_TYPE", "type phải là photo|note|video.");
}
