using FluentValidation;
using RoadMate.Application.Accounts.DTOs.Requests;

namespace RoadMate.Application.Accounts.Validators;

public sealed class UpdateProfileRequestValidator : AbstractValidator<UpdateProfileRequest>
{
  public UpdateProfileRequestValidator()
  {
    RuleFor(x => x.CrashSensitivity).ValidCrashSensitivity();
    RuleFor(x => x.IdleMinutes).ValidIdleMinutes();
  }
}
