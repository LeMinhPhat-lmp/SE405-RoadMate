namespace RoadMate.Services.Accounts;

/// <summary>Quy tắc cho các cài đặt an toàn; dùng chung cho hồ sơ người dùng và cài đặt riêng của từng chuyến.</summary>
public static class SafetySettingsValidator
{
  public static Result Validate(string? crashSensitivity, int? idleMinutes)
  {
    if (crashSensitivity is not null && !CrashSensitivity.All.Contains(crashSensitivity)) return Errors.InvalidSensitivity;
    if (idleMinutes is < 1 or > 240) return Errors.InvalidIdle;
    return Result.Success();
  }
}
