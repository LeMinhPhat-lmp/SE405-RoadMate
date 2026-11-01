namespace RoadMate.Controllers;

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
