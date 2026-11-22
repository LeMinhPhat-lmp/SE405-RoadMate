using RoadMate.Application.Abstractions.Messaging;
using RoadMate.Application.Groups.DTOs.Requests;
using RoadMate.Domain.Common;

namespace RoadMate.Application.Groups.Validators;

public sealed class JoinGroupRequestValidator : IRequestValidator<JoinGroupRequest>
{
  public Result Validate(JoinGroupRequest r, DateTimeOffset now) =>
    RoadMate.Domain.Groups.GroupCode.Normalize(r.Code) is null ? RoadMate.Domain.Groups.GroupErrors.InvalidCode : Result.Success();
}
