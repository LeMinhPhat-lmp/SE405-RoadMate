using FluentValidation;
using RoadMate.Application.Alerts.DTOs.Requests;
using RoadMate.Application.Common;
using RoadMate.Domain.Common;
using RoadMate.Domain.ValueObjects;

namespace RoadMate.Application.Alerts.Validators;

public sealed class AlertLocationRequestValidator : AbstractValidator<AlertLocationRequest>
{
  public AlertLocationRequestValidator()
  {
    RuleFor(x => x)
      .Must(r => GeoPoint.Create(r.Lat, r.Lng).IsSuccess)
      .OverridePropertyName("Location")
      .WithError(Errors.InvalidLocation);
  }
}
