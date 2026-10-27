namespace RoadMate.Controllers;

public class AuthController : ApiControllerBase
{
    private readonly IAuthService _svc;
    public AuthController(IAuthService svc) => _svc = svc;

    /// <summary>Gọi ngay sau khi đăng nhập Firebase: tạo/cập nhật user và lưu fcmToken.</summary>
    [HttpPost("auth/sync")]
    public async Task<IActionResult> Sync(SyncRequest req, CancellationToken ct) =>
        Handle(await _svc.SyncAsync(Uid, User.FindFirstValue("email"), User.FindFirstValue("name"), User.FindFirstValue("phone_number"), req, ct));

    [HttpGet("me")]
    public async Task<IActionResult> Me(CancellationToken ct) => Handle(await _svc.GetMeAsync(Uid, ct));

    [HttpPut("me")]
    public async Task<IActionResult> UpdateMe(UpdateProfileRequest req, CancellationToken ct) => Handle(await _svc.UpdateMeAsync(Uid, req, ct));
}

public class EmergencyContactsController : ApiControllerBase
{
    private readonly IEmergencyContactService _svc;
    public EmergencyContactsController(IEmergencyContactService svc) => _svc = svc;

    [HttpGet("emergency-contacts")]
    public async Task<IActionResult> List(CancellationToken ct) => Handle(await _svc.ListAsync(Uid, ct));

    [HttpPost("emergency-contacts")]
    public async Task<IActionResult> Create(ContactRequest req, CancellationToken ct) => HandleCreated(await _svc.CreateAsync(Uid, req, ct));

    [HttpPut("emergency-contacts/{id:guid}")]
    public async Task<IActionResult> Update(Guid id, ContactRequest req, CancellationToken ct) => Handle(await _svc.UpdateAsync(Uid, id, req, ct));

    [HttpDelete("emergency-contacts/{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct) => Handle(await _svc.DeleteAsync(Uid, id, ct));

    [HttpPost("emergency-contacts/{id:guid}/test")]
    public async Task<IActionResult> Test(Guid id, CancellationToken ct) => Handle(await _svc.TestAsync(Uid, id, ct));
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
