using FluentValidation;
using RoadMate.Application.Accounts.Validators;
using RoadMate.Application.Common;
using RoadMate.Application.Trips.DTOs.Requests;
using RoadMate.Domain.Common;
using RoadMate.Domain.ValueObjects;

namespace RoadMate.Application.Trips.Validators;

/// <summary>Order matches the API contract; the waypoint cap also lives in the aggregate.</summary>
public sealed class TripUpsertRequestValidator : AbstractValidator<TripUpsertRequest>
{
  public const int MaxTitleLength = 200;

  public TripUpsertRequestValidator()
  {
    RuleFor(x => x.Title)
      .Cascade(CascadeMode.Stop)
      .NotEmpty()
        .WithErrorCode("INVALID_TITLE")
        .WithMessage("Tên chuyến bắt buộc (≤200 ký tự).")
      .MaximumLength(MaxTitleLength)
        .WithErrorCode("INVALID_TITLE")
        .WithMessage("Tên chuyến bắt buộc (≤200 ký tự).");

    RuleFor(x => x.Origin)
      .Must(o => o is not null && GeoPoint.Create(o.Lat, o.Lng).IsSuccess)
      .WithError(Errors.InvalidLocation);

    RuleFor(x => x.Destination)
      .Must(d => d is not null && GeoPoint.Create(d.Lat, d.Lng).IsSuccess)
      .WithError(Errors.InvalidLocation);

    RuleFor(x => x.Waypoints)
      .Must(w => w is null || w.Count <= Errors.MaxWaypoints)
      .WithError(Errors.TooManyWaypoints);

    RuleForEach(x => x.Waypoints)
      .Must(w => w is not null && GeoPoint.Create(w.Lat, w.Lng).IsSuccess)
      .WithError(Errors.InvalidLocation)
      .When(x => x.Waypoints is not null);

    RuleFor(x => x.CrashSensitivity).ValidCrashSensitivity();
    RuleFor(x => x.IdleMinutes).ValidIdleMinutes();
  }
}
