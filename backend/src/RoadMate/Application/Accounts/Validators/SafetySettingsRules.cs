using RoadMate.Domain.Common;
using RoadMate.Domain.Users;

namespace RoadMate.Application.Accounts.Validators;

/// <summary>Shared by the profile and per-trip safety settings.</summary>
public static class SafetySettingsRules
{
  public static Result Check(string? crashSensitivity, int? idleMinutes)
  {
    if (crashSensitivity is not null && !EnumWire.TryParse<CrashSensitivity>(crashSensitivity, out _)) return Errors.InvalidSensitivity;
    if (idleMinutes is < 1 or > 240) return Errors.InvalidIdle;
    return Result.Success();
  }
}
