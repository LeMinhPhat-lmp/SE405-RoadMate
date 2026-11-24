using FluentValidation;
using RoadMate.Application.Common;
using RoadMate.Application.Trips.DTOs.Requests;
using RoadMate.Domain.Common;

namespace RoadMate.Application.Trips.Validators;

public sealed class StartTripRequestValidator : AbstractValidator<StartTripRequest>
{
  public StartTripRequestValidator(TimeProvider clock)
  {
    RuleFor(x => x.ExpectedReturnTime)
      .Must(t => t!.Value.ToUtcOffset() > clock.GetUtcNow())
      .When(x => x.ExpectedReturnTime.HasValue)
      .WithError(Errors.InvalidReturnTime);
  }
}
