using FluentValidation;
using RoadMate.Application.Common;
using RoadMate.Application.Maps.DTOs.Requests;
using RoadMate.Application.Maps.Implements;
using RoadMate.Domain.Common;
using RoadMate.Domain.ValueObjects;

namespace RoadMate.Application.Maps.Validators;

public sealed class DirectionsRequestValidator : AbstractValidator<DirectionsRequest>
{
  public DirectionsRequestValidator()
  {
    RuleFor(x => x)
      .Must(r => r.Origin is not null && r.Destination is not null)
      .OverridePropertyName("Points")
      .WithErrorCode("MISSING_POINTS")
      .WithMessage("Thiếu điểm đi hoặc điểm đến.");

    When(x => x.Origin is not null && x.Destination is not null, () =>
    {
      RuleFor(x => x.Waypoints)
        .Must(w => w is null || w.Count <= RoutingService.MaxWaypoints)
        .WithErrorCode("TOO_MANY_WAYPOINTS")
        .WithMessage($"Tối đa {RoutingService.MaxWaypoints} điểm dừng.");

      RuleFor(x => x.Origin)
        .Must(p => GeoPoint.Create(p.Lat, p.Lng).IsSuccess)
        .WithError(Errors.InvalidLocation);

      RuleForEach(x => x.Waypoints)
        .Must(p => p is not null && GeoPoint.Create(p.Lat, p.Lng).IsSuccess)
        .WithError(Errors.InvalidLocation)
        .When(x => x.Waypoints is not null);

      RuleFor(x => x.Destination)
        .Must(p => GeoPoint.Create(p.Lat, p.Lng).IsSuccess)
        .WithError(Errors.InvalidLocation);
    });

    RuleFor(x => x.Vehicle)
      .Must(VehicleMap.IsValid)
      .WithErrorCode("INVALID_VEHICLE")
      .WithMessage("vehicle phải là motorbike hoặc car.");
  }
}
