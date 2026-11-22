using RoadMate.Application.Abstractions.Messaging;
using RoadMate.Application.Groups.DTOs.Requests;
using RoadMate.Domain.Common;

namespace RoadMate.Application.Groups.Validators;

public sealed class CreateGroupRequestValidator : IRequestValidator<CreateGroupRequest>
{
  public const int MaxNameLength = 100;
  public Result Validate(CreateGroupRequest r, DateTimeOffset now) =>
    string.IsNullOrWhiteSpace(r.Name) || r.Name.Trim().Length > MaxNameLength
      ? Error.Validation("INVALID_NAME", $"Tên nhóm bắt buộc, tối đa {MaxNameLength} ký tự.")
      : Result.Success();   // meeting-point rules are enforced by Group.Create
}
