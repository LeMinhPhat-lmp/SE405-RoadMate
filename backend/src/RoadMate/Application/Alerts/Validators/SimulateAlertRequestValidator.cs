using FluentValidation;
using Microsoft.Extensions.Options;
using RoadMate.Application.Alerts.DTOs.Requests;
using RoadMate.Application.Common;
using RoadMate.Domain.Alerts;
using RoadMate.Domain.Common;
using RoadMate.Domain.ValueObjects;

namespace RoadMate.Application.Alerts.Validators;

public sealed class SimulateAlertRequestValidator : AbstractValidator<SimulateAlertRequest>
{
  private const double DefaultLat = 10.7769;
  private const double DefaultLng = 106.7009;

  public SimulateAlertRequestValidator(IOptions<DebugOptions> debug)
  {
    // Keep 404 SIMULATOR_DISABLED precedence when the debug simulator is turned off.
    When(_ => debug.Value.EnableSimulator, () =>
    {
      RuleFor(x => x.Type)
        .Must(t => t is null || EnumWire.TryParse<AlertType>(t, out _))
        .WithErrorCode("INVALID_TYPE")
        .WithMessage($"type phải thuộc: {EnumWire.JoinWire<AlertType>()}.");

      RuleFor(x => x)
        .Must(r => GeoPoint.Create(r.Lat ?? DefaultLat, r.Lng ?? DefaultLng).IsSuccess)
        .OverridePropertyName("Location")
        .WithError(Errors.InvalidLocation);
    });
  }
}
