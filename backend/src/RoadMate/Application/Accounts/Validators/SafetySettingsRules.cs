using FluentValidation;
using RoadMate.Application.Common;
using RoadMate.Domain.Common;
using RoadMate.Domain.Users;

namespace RoadMate.Application.Accounts.Validators;

/// <summary>Shared FluentValidation rules for profile and per-trip safety settings.</summary>
public static class SafetySettingsRules
{
  public static IRuleBuilderOptions<T, string?> ValidCrashSensitivity<T>(this IRuleBuilder<T, string?> rule) =>
    rule.Must(v => v is null || EnumWire.TryParse<CrashSensitivity>(v, out _)).WithError(Errors.InvalidSensitivity);

  public static IRuleBuilderOptions<T, int?> ValidIdleMinutes<T>(this IRuleBuilder<T, int?> rule) =>
    rule.InclusiveBetween(1, 240).WithError(Errors.InvalidIdle);
}
