using Microsoft.AspNetCore.Mvc.Filters;

namespace RoadMate.Infrastructure.Auth;

/// <summary>Filter toàn cục: user đã đăng nhập nhưng chưa /auth/sync thì trả 409 USER_NOT_SYNCED.</summary>
public class SyncedUserFilter : IAsyncActionFilter
{
  public Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
  {
    var user = context.HttpContext.User;
    var skip = context.ActionDescriptor.EndpointMetadata.Any(m => m is AllowUnsyncedUserAttribute or IAllowAnonymous);
    if (skip || user.Identity?.IsAuthenticated != true || user.HasClaim(c => c.Type == UserClaims.InternalId)) return next();
    context.Result = Errors.UserNotSynced.ToProblem();
    return Task.CompletedTask;
  }
}
