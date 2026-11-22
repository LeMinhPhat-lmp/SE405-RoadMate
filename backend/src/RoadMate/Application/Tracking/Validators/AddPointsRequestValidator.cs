using RoadMate.Application.Abstractions.Messaging;
using RoadMate.Application.Tracking.DTOs.Requests;
using RoadMate.Domain.Common;

namespace RoadMate.Application.Tracking.Validators;

public sealed class AddPointsRequestValidator : IRequestValidator<AddPointsRequest>
{
  public const int MaxBatch = 500;
  public Result Validate(AddPointsRequest req, DateTimeOffset now)
  {
    if (req.Points.Count > MaxBatch) return Error.Validation("BATCH_TOO_LARGE", $"Tối đa {MaxBatch} điểm mỗi lần gửi.");
    return req.Points.Any(p => !Geo.ValidLatLng(p.Lat, p.Lng)) ? Errors.InvalidLocation : Result.Success();
  }
}
