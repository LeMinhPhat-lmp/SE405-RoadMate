namespace RoadMate.Services;

public interface IAuthService
{
  Task<Result<UserDto>> SyncAsync(string uid, string? email, string? name, string? phone, SyncRequest req, CancellationToken ct);
  Task<Result<UserDto>> GetMeAsync(string uid, CancellationToken ct);
  Task<Result<UserDto>> UpdateMeAsync(string uid, UpdateProfileRequest req, CancellationToken ct);
}

public class AuthService : IAuthService
{
  private static readonly string[] Sens = { "low", "medium", "high" };
  private readonly IUnitOfWork _uow;
  public AuthService(IUnitOfWork uow) => _uow = uow;

  public async Task<Result<UserDto>> SyncAsync(string uid, string? email, string? name, string? phone, SyncRequest req, CancellationToken ct)
  {
    var user = await _uow.Users.GetByUidAsync(uid, ct);
    if (user is null)
    {
      user = new User
      {
        Uid = uid,
        Email = string.IsNullOrWhiteSpace(email) ? null : email,
        Name = req.Name ?? name ?? email?.Split('@')[0] ?? "Người dùng",
        CreatedAt = DateTime.UtcNow
      };
      await _uow.Users.AddAsync(user, ct);
    }
    if (!string.IsNullOrWhiteSpace(req.FcmToken)) user.FcmToken = req.FcmToken;

    // Liên kết các liên hệ khẩn cấp có cùng số điện thoại ĐÃ XÁC THỰC (claim phone_number) để nhận push.
    if (!string.IsNullOrWhiteSpace(phone))
    {
      user.Phone = phone;
      foreach (var c in await _uow.EmergencyContacts.ListByPhoneAsync(phone, ct))
        if (c.UserId != uid) c.LinkedUserId = uid;
    }
    await _uow.SaveChangesAsync(ct);
    return user.ToDto();
  }

  public async Task<Result<UserDto>> GetMeAsync(string uid, CancellationToken ct)
  {
    var u = await _uow.Users.GetByUidAsync(uid, ct);
    return u is null ? Errors.UserNotSynced : u.ToDto();
  }

  public async Task<Result<UserDto>> UpdateMeAsync(string uid, UpdateProfileRequest r, CancellationToken ct)
  {
    var u = await _uow.Users.GetByUidAsync(uid, ct);
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
    await _uow.SaveChangesAsync(ct);
    return u.ToDto();
  }
}
