using FluentValidation;
using RoadMate.Application.Common;
using RoadMate.Application.Tracking.DTOs.Requests;
using RoadMate.Domain.Common;
using RoadMate.Domain.ValueObjects;

namespace RoadMate.Application.Tracking.Validators;

public sealed class HeartbeatRequestValidator : AbstractValidator<HeartbeatRequest>
{
  public HeartbeatRequestValidator()
  {
    RuleFor(x => x)
      .Must(r => GeoPoint.Create(r.Lat, r.Lng).IsSuccess)
      .OverridePropertyName("Location")
      .WithError(Errors.InvalidLocation);
  }
}
