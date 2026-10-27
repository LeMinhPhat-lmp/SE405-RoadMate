namespace RoadMate.Controllers;

[ApiController]
[Authorize]
[Route("api/v1")]
public abstract class ApiControllerBase : ControllerBase
{
  /// <summary>Firebase UID (claim user_id, fallback sub). Chỉ dùng cho /auth/sync.</summary>
  protected string FirebaseUid => User.FindFirstValue(UserClaims.FirebaseUid) ?? User.FindFirstValue("sub") ?? throw new UnauthorizedAccessException();

  /// <summary>Id nội bộ (Guid) của User. SyncedUserFilter đảm bảo claim này tồn tại ở mọi endpoint cần đăng nhập (trừ /auth/sync).</summary>
  protected Guid UserId => Guid.TryParse(User.FindFirstValue(UserClaims.InternalId), out var id) ? id : throw new UnauthorizedAccessException();

  protected IActionResult Handle(Result r) => r.IsSuccess ? NoContent() : r.Error.ToProblem();
  protected IActionResult Handle<T>(Result<T> r) => r.IsSuccess ? Ok(r.Value) : r.Error.ToProblem();
  protected IActionResult HandleCreated<T>(Result<T> r) => r.IsSuccess ? StatusCode(StatusCodes.Status201Created, r.Value) : r.Error.ToProblem();
}
