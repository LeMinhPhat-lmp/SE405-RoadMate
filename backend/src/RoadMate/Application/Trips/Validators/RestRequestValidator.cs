using FluentValidation;
using RoadMate.Application.Common;
using RoadMate.Application.Trips.DTOs.Requests;
using RoadMate.Domain.Common;

namespace RoadMate.Application.Trips.Validators;

/// <summary>Request validator: checks rest range before loading the trip.</summary>
public sealed class RestRequestValidator : AbstractValidator<RestRequest>
{
  public RestRequestValidator()
  {
    RuleFor(x => x.Minutes)
      .InclusiveBetween(5, 480)
      .WithError(Errors.InvalidRest);
  }
}
