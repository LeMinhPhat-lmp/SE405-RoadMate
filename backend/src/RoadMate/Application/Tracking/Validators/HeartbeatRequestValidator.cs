using RoadMate.Application.Abstractions.Messaging;
using RoadMate.Application.Tracking.DTOs.Requests;
using RoadMate.Domain.Common;

namespace RoadMate.Application.Tracking.Validators;

public sealed class HeartbeatRequestValidator : IRequestValidator<HeartbeatRequest>
{
  public Result Validate(HeartbeatRequest r, DateTimeOffset now) => Geo.ValidLatLng(r.Lat, r.Lng) ? Result.Success() : Errors.InvalidLocation;
}
