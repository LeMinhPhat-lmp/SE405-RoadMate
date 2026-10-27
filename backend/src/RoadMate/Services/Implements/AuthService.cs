namespace RoadMate.Services.Implements;

public class AuthService(IUnitOfWork uow) : IAuthService
{
  private static readonly string[] Sens = { "low", "medium", "high" };

  public async Task<Result<UserDto>> SyncAsync(string firebaseUid, string? email, string? name, string? phone, SyncRequest req, CancellationToken ct)
  {
    var user = await uow.UserRepository.GetByFirebaseUidAsync(firebaseUid, ct);
    if (user is null)
    {
      user = new User
      {
        Id = Guid.NewGuid(),
        FirebaseUid = firebaseUid,
        Email = string.IsNullOrWhiteSpace(email) ? null : email,
        Name = req.Name ?? name ?? email?.Split('@')[0] ?? "Người dùng"
      };
      await uow.UserRepository.AddAsync(user, ct);
    }
    if (!string.IsNullOrWhiteSpace(req.FcmToken)) user.FcmToken = req.FcmToken;

    // Liên kết các liên hệ khẩn cấp có cùng số điện thoại ĐÃ XÁC THỰC (claim phone_number) để nhận push.
    if (!string.IsNullOrWhiteSpace(phone))
    {
      user.Phone = phone;
      foreach (var c in await uow.EmergencyContactRepository.ListByPhoneAsync(phone, ct))
        if (c.UserId != user.Id) c.LinkedUserId = user.Id;
    }
    await uow.SaveChangesAsync(ct);
    return user.ToDto();
  }

  public async Task<Result<UserDto>> GetMeAsync(Guid userId, CancellationToken ct)
  {
    var u = await uow.UserRepository.GetByIdAsync(userId, false, ct);
    return u is null ? Errors.UserNotSynced : u.ToDto();
  }

  public async Task<Result<UserDto>> UpdateMeAsync(Guid userId, UpdateProfileRequest r, CancellationToken ct)
  {
    var u = await uow.UserRepository.GetByIdAsync(userId, true, ct);
    if (u is null) return Errors.UserNotSynced;
    if (r.CrashSensitivity is not null && !Sens.Contains(r.CrashSensitivity))
      return Error.Validation("INVALID_SENSITIVITY", "crashSensitivity phải là low|medium|high.");
    if (r.IdleMinutes is < 1 or > 240) return Error.Validation("INVALID_IDLE", "idleMinutes phải từ 1 đến 240.");
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
    await uow.SaveChangesAsync(ct);
    return u.ToDto();
  }
}
