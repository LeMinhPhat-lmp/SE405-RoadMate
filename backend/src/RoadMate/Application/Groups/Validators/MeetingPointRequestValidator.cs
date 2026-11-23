using FluentValidation;
using RoadMate.Application.Common;
using RoadMate.Application.Groups.DTOs.Requests;
using RoadMate.Domain.Common;
using RoadMate.Domain.Groups;
using RoadMate.Domain.ValueObjects;

namespace RoadMate.Application.Groups.Validators;

public sealed class MeetingPointRequestValidator : AbstractValidator<MeetingPointRequest>
{
  public MeetingPointRequestValidator(TimeProvider clock)
  {
    RuleFor(x => x)
      .Must(r => GeoPoint.Create(r.Lat, r.Lng).IsSuccess)
      .OverridePropertyName("Location")
      .WithError(Errors.InvalidLocation);

    RuleFor(x => x.DepartAt)
      .Must(d => d!.Value.ToUtcOffset() > clock.GetUtcNow())
      .When(x => x.DepartAt.HasValue)
      .WithError(GroupErrors.InvalidDepartTime);
  }
}
