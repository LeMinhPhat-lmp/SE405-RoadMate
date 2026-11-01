namespace RoadMate.Services.Accounts;

public class AuthService(IUnitOfWork uow) : IAuthService
{
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
    var valid = SafetySettingsValidator.Validate(r.CrashSensitivity, r.IdleMinutes);
    if (valid.IsFailure) return valid.Error;
    u.ApplyProfile(r);
    await uow.SaveChangesAsync(ct);
    return u.ToDto();
  }
}
