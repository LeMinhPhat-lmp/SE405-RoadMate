using RoadMate.Application.Abstractions.Messaging;
using RoadMate.Application.Accounts.DTOs.Requests;
using RoadMate.Domain.Common;

namespace RoadMate.Application.Accounts.Validators;

public sealed class UpdateProfileRequestValidator : IRequestValidator<UpdateProfileRequest>
{
  public Result Validate(UpdateProfileRequest r, DateTimeOffset now) => SafetySettingsRules.Check(r.CrashSensitivity, r.IdleMinutes);
}
