using FluentValidation;
using RoadMate.Application.Common;
using RoadMate.Application.Maps.DTOs.Requests;
using RoadMate.Domain.Common;
using RoadMate.Domain.ValueObjects;

namespace RoadMate.Application.Maps.Validators;

public sealed class RefreshRouteRequestValidator : AbstractValidator<RefreshRouteRequest>
{
  public RefreshRouteRequestValidator()
  {
    RuleFor(x => x.From)
      .Must(p => GeoPoint.Create(p!.Lat, p.Lng).IsSuccess)
      .When(x => x.From is not null)
      .WithError(Errors.InvalidLocation);

    RuleFor(x => x.Vehicle)
      .Must(VehicleMap.IsValid)
      .WithErrorCode("INVALID_VEHICLE")
      .WithMessage("vehicle phải là motorbike hoặc car.");
  }
}
