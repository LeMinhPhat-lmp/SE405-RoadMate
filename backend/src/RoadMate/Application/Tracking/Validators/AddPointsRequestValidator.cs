using FluentValidation;
using RoadMate.Application.Common;
using RoadMate.Application.Tracking.DTOs.Requests;
using RoadMate.Domain.Common;
using RoadMate.Domain.ValueObjects;

namespace RoadMate.Application.Tracking.Validators;

public sealed class AddPointsRequestValidator : AbstractValidator<AddPointsRequest>
{
  public const int MaxBatch = 500;

  public AddPointsRequestValidator()
  {
    RuleFor(x => x.Points)
      .Cascade(CascadeMode.Stop)
      .NotNull()
        .WithErrorCode("INVALID_POINTS")
        .WithMessage("Danh sách điểm không được để trống.")
      .Must(p => p.Count <= MaxBatch)
        .WithErrorCode("BATCH_TOO_LARGE")
        .WithMessage($"Tối đa {MaxBatch} điểm mỗi lần gửi.");

    RuleForEach(x => x.Points)
      .Must(p => p is not null && GeoPoint.Create(p.Lat, p.Lng).IsSuccess)
      .WithError(Errors.InvalidLocation)
      .When(x => x.Points is not null);
  }
}
