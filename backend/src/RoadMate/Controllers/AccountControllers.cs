namespace RoadMate.Controllers;

public class AuthController(IAuthService svc) : ApiControllerBase
{
  /// <summary>Gọi ngay sau khi đăng nhập Firebase: tạo/cập nhật user và lưu fcmToken.</summary>
  [AllowUnsyncedUser]
  [HttpPost("auth/sync")]
  public async Task<IActionResult> Sync(SyncRequest req, CancellationToken ct) =>
    Handle(await svc.SyncAsync(FirebaseUid, User.FindFirstValue("email"), User.FindFirstValue("name"), User.FindFirstValue("phone_number"), req, ct));

  [HttpGet("me")]
  public async Task<IActionResult> Me(CancellationToken ct) => Handle(await svc.GetMeAsync(UserId, ct));

  [HttpPut("me")]
  public async Task<IActionResult> UpdateMe(UpdateProfileRequest req, CancellationToken ct) => Handle(await svc.UpdateMeAsync(UserId, req, ct));
}

public class EmergencyContactsController(IEmergencyContactService svc) : ApiControllerBase
{
  [HttpGet("emergency-contacts")]
  public async Task<IActionResult> List(CancellationToken ct) => Handle(await svc.ListAsync(UserId, ct));

  [HttpPost("emergency-contacts")]
  public async Task<IActionResult> Create(ContactRequest req, CancellationToken ct) => HandleCreated(await svc.CreateAsync(UserId, req, ct));

  [HttpPut("emergency-contacts/{id:guid}")]
  public async Task<IActionResult> Update(Guid id, ContactRequest req, CancellationToken ct) => Handle(await svc.UpdateAsync(UserId, id, req, ct));

  [HttpDelete("emergency-contacts/{id:guid}")]
  public async Task<IActionResult> Delete(Guid id, CancellationToken ct) => Handle(await svc.DeleteAsync(UserId, id, ct));

  /// <remarks>
  /// ⚠ CẦN CẤU HÌNH: mặc định Sms:Provider=log nên chỉ GHI LOG, không có SMS thật. Push chỉ gửi khi có Firebase:CredentialPath
  /// và liên hệ đã liên kết tài khoản (số điện thoại Firebase đã xác thực). Xem docs/SETUP_AND_LIMITATIONS.md.
  /// </remarks>
  [HttpPost("emergency-contacts/{id:guid}/test")]
  public async Task<IActionResult> Test(Guid id, CancellationToken ct) => Handle(await svc.TestAsync(UserId, id, ct));
}

public class ConfigController : ApiControllerBase
{
  [AllowAnonymous]
  [HttpGet("config/emergency-numbers")]
  public IActionResult EmergencyNumbers() => Ok(new[]
  {
    new { number = "113", service = "Công an" }, new { number = "114", service = "Cứu hỏa, cứu nạn cứu hộ" },
    new { number = "115", service = "Cấp cứu y tế" }, new { number = "112", service = "Số khẩn cấp thống nhất" }
  });
}
