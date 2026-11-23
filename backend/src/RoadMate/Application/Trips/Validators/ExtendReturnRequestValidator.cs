using FluentValidation;
using RoadMate.Application.Common;
using RoadMate.Application.Trips.DTOs.Requests;
using RoadMate.Domain.Common;

namespace RoadMate.Application.Trips.Validators;

public sealed class ExtendReturnRequestValidator : AbstractValidator<ExtendReturnRequest>
{
  public ExtendReturnRequestValidator(TimeProvider clock)
  {
    RuleFor(x => x.ExpectedReturnTime)
      .Must(t => t.ToUtcOffset() > clock.GetUtcNow())
      .WithError(Errors.InvalidReturnTime);
  }
}
