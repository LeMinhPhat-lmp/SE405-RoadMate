using FluentValidation;
using RoadMate.Application.Alerts.DTOs.Requests;
using RoadMate.Application.Common;
using RoadMate.Domain.Alerts;
using RoadMate.Domain.Common;
using RoadMate.Domain.ValueObjects;

namespace RoadMate.Application.Alerts.Validators;

/// <summary>Order of checks (and codes/messages) matches the API contract.</summary>
public sealed class CreateAlertRequestValidator : AbstractValidator<CreateAlertRequest>
{
  public CreateAlertRequestValidator()
  {
    RuleFor(x => x.ClientId)
      .Cascade(CascadeMode.Stop)
      .NotEmpty().WithError(Errors.InvalidClientId)
      .MaximumLength(ClientRequestId.MaxLength).WithError(Errors.InvalidClientId);

    RuleFor(x => x.Type)
      .Must(t => EnumWire.TryParse<AlertType>(t, out _))
      .WithErrorCode("INVALID_TYPE")
      .WithMessage($"type phải thuộc: {EnumWire.JoinWire<AlertType>()}.");

    RuleFor(x => x)
      .Must(r => GeoPoint.Create(r.Lat, r.Lng).IsSuccess)
      .OverridePropertyName("Location")
      .WithError(Errors.InvalidLocation);
  }
}
