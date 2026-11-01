namespace RoadMate.Dtos.Accounts;

public static class AccountMappers
{
  public static UserDto ToDto(this User u) => new(u.Id, u.Name, u.Email, u.Phone, u.AvatarUrl, u.BloodType, u.MedicalNotes,
    u.VehicleType, u.PlateNumber, u.IdleMinutes, u.CrashSensitivity, u.Language, u.Theme);

  /// <summary>Cập nhật hồ sơ: field null trong request nghĩa là giữ nguyên giá trị cũ.</summary>
  public static void ApplyProfile(this User u, UpdateProfileRequest r)
  {
    if (!string.IsNullOrWhiteSpace(r.Name)) u.Name = r.Name.Trim();
    u.AvatarUrl = r.AvatarUrl ?? u.AvatarUrl;
    u.BloodType = r.BloodType ?? u.BloodType;
    u.MedicalNotes = r.MedicalNotes ?? u.MedicalNotes;
    u.VehicleType = r.VehicleType ?? u.VehicleType;
    u.PlateNumber = r.PlateNumber ?? u.PlateNumber;
    u.IdleMinutes = r.IdleMinutes ?? u.IdleMinutes;
    u.CrashSensitivity = r.CrashSensitivity ?? u.CrashSensitivity;
    u.Language = r.Language ?? u.Language;
    u.Theme = r.Theme ?? u.Theme;
    if (!string.IsNullOrWhiteSpace(r.FcmToken)) u.FcmToken = r.FcmToken;
  }
}
