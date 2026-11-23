using FluentValidation;
using RoadMate.Application.Common;
using RoadMate.Application.Groups.DTOs.Requests;
using RoadMate.Domain.Common;
using RoadMate.Domain.Groups;
using RoadMate.Domain.ValueObjects;

namespace RoadMate.Application.Groups.Validators;

public sealed class CreateGroupRequestValidator : AbstractValidator<CreateGroupRequest>
{
  public const int MaxNameLength = 100;

  public CreateGroupRequestValidator(TimeProvider clock)
  {
    RuleFor(x => x.Name)
      .Cascade(CascadeMode.Stop)
      .NotEmpty()
        .WithErrorCode("INVALID_NAME")
        .WithMessage($"Tên nhóm bắt buộc, tối đa {MaxNameLength} ký tự.")
      .Must(n => n.Trim().Length <= MaxNameLength)
        .WithErrorCode("INVALID_NAME")
        .WithMessage($"Tên nhóm bắt buộc, tối đa {MaxNameLength} ký tự.");

    RuleFor(x => x)
      .Must(r => r.MeetingLat.HasValue == r.MeetingLng.HasValue &&
        (!r.MeetingLat.HasValue || GeoPoint.Create(r.MeetingLat.Value, r.MeetingLng!.Value).IsSuccess))
      .OverridePropertyName("MeetingLocation")
      .WithError(Errors.InvalidLocation);

    RuleFor(x => x.DepartAt)
      .Must(d => d!.Value.ToUtcOffset() > clock.GetUtcNow())
      .When(x => x.DepartAt.HasValue)
      .WithError(GroupErrors.InvalidDepartTime);
  }
}
