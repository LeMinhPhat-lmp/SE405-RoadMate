using Microsoft.Extensions.Options;
using RoadMate.Application.Abstractions.Persistence;
using RoadMate.Application.Common;
using RoadMate.Application.Tracking;
using RoadMate.Application.Trips.DTOs.Requests;
using RoadMate.Domain.Common;
using RoadMate.Domain.Trips;

namespace RoadMate.Application.Trips.Validators;

/// <summary>Request validator: legacy checked the rest range BEFORE loading the trip, so it stays a request-shape rule.</summary>
public sealed class RestRequestValidator : RoadMate.Application.Abstractions.Messaging.IRequestValidator<RestRequest>
{
  public Result Validate(RestRequest r, DateTimeOffset now) => r.Minutes is < 5 or > 480 ? Errors.InvalidRest : Result.Success();
}
