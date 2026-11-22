using RoadMate.Application.Abstractions.Messaging;
using RoadMate.Application.Alerts.DTOs.Requests;
using RoadMate.Domain.Alerts;
using RoadMate.Domain.Common;
using RoadMate.Domain.ValueObjects;

namespace RoadMate.Application.Alerts.Validators;

/// <summary>Order of checks (and codes/messages) identical to the legacy static validator.</summary>
public sealed class CreateAlertRequestValidator : IRequestValidator<CreateAlertRequest>
{
  public Result Validate(CreateAlertRequest req, DateTimeOffset now)
  {
    if (ClientRequestId.Create(req.ClientId).IsFailure || req.ClientId.Length > ClientRequestId.MaxLength) return Errors.InvalidClientId;
    if (!EnumWire.TryParse<AlertType>(req.Type, out _)) return Error.Validation("INVALID_TYPE", $"type phải thuộc: {EnumWire.JoinWire<AlertType>()}.");
    return GeoPoint.Create(req.Lat, req.Lng).IsFailure ? Errors.InvalidLocation : Result.Success();
  }
}
