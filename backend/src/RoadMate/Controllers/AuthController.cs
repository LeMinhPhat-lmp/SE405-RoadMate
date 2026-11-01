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
