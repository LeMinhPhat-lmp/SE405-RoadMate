using FluentValidation;
using RoadMate.Application.Common;
using RoadMate.Application.Maps.DTOs.Requests;
using RoadMate.Application.Maps.Implements;
using RoadMate.Domain.Common;
using RoadMate.Domain.ValueObjects;

namespace RoadMate.Application.Maps.Validators;

public sealed class DistanceMatrixRequestValidator : AbstractValidator<DistanceMatrixRequest>
{
  public DistanceMatrixRequestValidator()
  {
    RuleFor(x => x)
      .Must(r => r.Origins is { Count: > 0 } && r.Destinations is { Count: > 0 })
      .OverridePropertyName("Points")
      .WithErrorCode("MISSING_POINTS")
      .WithMessage("Cần ít nhất 1 điểm xuất phát và 1 điểm đến.");

    When(x => x.Origins is { Count: > 0 } && x.Destinations is { Count: > 0 }, () =>
    {
      RuleFor(x => x)
        .Must(r => r.Origins.Count <= RoutingService.MaxMatrixOrigins && r.Destinations.Count <= RoutingService.MaxMatrixDestinations)
        .OverridePropertyName("Points")
        .WithErrorCode("TOO_MANY_POINTS")
        .WithMessage($"Tối đa {RoutingService.MaxMatrixOrigins} điểm xuất phát và {RoutingService.MaxMatrixDestinations} điểm đến.");

      RuleForEach(x => x.Origins)
        .Must(p => p is not null && GeoPoint.Create(p.Lat, p.Lng).IsSuccess)
        .WithError(Errors.InvalidLocation);

      RuleForEach(x => x.Destinations)
        .Must(p => p is not null && GeoPoint.Create(p.Lat, p.Lng).IsSuccess)
        .WithError(Errors.InvalidLocation);
    });

    RuleFor(x => x.Vehicle)
      .Must(VehicleMap.IsValid)
      .WithErrorCode("INVALID_VEHICLE")
      .WithMessage("vehicle phải là motorbike hoặc car.");
  }
}
