namespace RoadMate.Controllers;

[ApiController]
[Authorize]
[Route("api/v1")]
public abstract class ApiControllerBase : ControllerBase
{
    /// <summary>Firebase UID (claim user_id, fallback sub).</summary>
    protected string Uid => User.FindFirstValue("user_id") ?? User.FindFirstValue("sub") ?? throw new UnauthorizedAccessException();

    protected IActionResult Handle(Result r) => r.IsSuccess ? NoContent() : r.Error.ToProblem();
    protected IActionResult Handle<T>(Result<T> r) => r.IsSuccess ? Ok(r.Value) : r.Error.ToProblem();
    protected IActionResult HandleCreated<T>(Result<T> r) => r.IsSuccess ? StatusCode(StatusCodes.Status201Created, r.Value) : r.Error.ToProblem();
}
