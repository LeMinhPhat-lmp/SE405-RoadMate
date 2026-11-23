using FluentValidation;
using RoadMate.Application.Common;
using RoadMate.Application.Groups.DTOs.Requests;
using RoadMate.Domain.Groups;

namespace RoadMate.Application.Groups.Validators;

public sealed class JoinGroupRequestValidator : AbstractValidator<JoinGroupRequest>
{
  public JoinGroupRequestValidator()
  {
    RuleFor(x => x.Code)
      .Must(c => GroupCode.Normalize(c) is not null)
      .WithError(GroupErrors.InvalidCode);
  }
}
