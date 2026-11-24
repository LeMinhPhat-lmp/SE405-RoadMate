using RoadMate.Application.Accounts.DTOs.Responses;
using RoadMate.Application.Accounts.DTOs.Requests;
using RoadMate.Domain.Common;
using RoadMate.Domain.Users;

namespace RoadMate.Application.Accounts.Mappers;

public static class AccountMappers
{
  public static UserResponse ToResponse(this User u) => new(u.Id, u.Name, u.Email, u.Phone, u.AvatarUrl, u.BloodType, u.MedicalNotes,
    u.VehicleType, u.PlateNumber, u.IdleMinutes, EnumWire.ToWire(u.CrashSensitivity), u.Language, u.Theme);

  /// <summary>Request → domain update (null = keep). Sensitivity was already validated by the request validator.</summary>
  public static ProfileUpdate ToUpdate(this UpdateProfileRequest r) =>
    new(r.Name, r.AvatarUrl, r.BloodType, r.MedicalNotes, r.VehicleType, r.PlateNumber, r.IdleMinutes,
      EnumWire.TryParse<CrashSensitivity>(r.CrashSensitivity, out var s) ? s : null, r.Language, r.Theme, r.FcmToken);
}
