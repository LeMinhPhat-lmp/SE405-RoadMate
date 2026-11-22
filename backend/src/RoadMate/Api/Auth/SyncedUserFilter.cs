using Microsoft.AspNetCore.Mvc.Filters;
using RoadMate.Domain.Common;
using RoadMate.Api.Http;
using Microsoft.AspNetCore.Authorization;

namespace RoadMate.Api.Auth;

public class SyncedUserFilter : IAsyncActionFilter
{
  public Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
  {
    var user = context.HttpContext.User;
    var skip = context.ActionDescriptor.EndpointMetadata.Any(m => m is AllowUnsyncedUserAttribute or IAllowAnonymous);
    if (skip || user.Identity?.IsAuthenticated != true || user.HasClaim(c => c.Type == UserClaims.InternalId)) return next();
    context.Result = Errors.UserNotSynced.ToProblem(context.HttpContext);
    return Task.CompletedTask;
  }
}
