using FluentValidation;
using RoadMate.Application.Journal.DTOs.Requests;
using RoadMate.Domain.Common;
using RoadMate.Domain.Trips;

namespace RoadMate.Application.Journal.Validators;

/// <summary>Only the request-shape rule (type parsing) stays here; note/media/rating/location invariants live in <c>Memory</c>.</summary>
public sealed class MemoryRequestValidator : AbstractValidator<MemoryRequest>
{
  public MemoryRequestValidator()
  {
    RuleFor(x => x.Type)
      .Must(t => EnumWire.TryParse<MemoryType>(t, out _))
      .WithErrorCode("INVALID_TYPE")
      .WithMessage("type phải là photo|note|video.");
  }
}
