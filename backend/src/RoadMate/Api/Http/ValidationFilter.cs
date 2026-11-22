using Microsoft.AspNetCore.Mvc.Filters;
using RoadMate.Application.Abstractions.Messaging;

namespace RoadMate.Api.Http;

/// <summary>
/// Runs the registered <c>IRequestValidator&lt;T&gt;</c> for every action argument before the controller executes and answers
/// 400 ProblemDetails (same code/message as before) on failure. Controllers never validate by hand.
/// </summary>
public sealed class ValidationFilter(IServiceProvider services, TimeProvider clock) : IAsyncActionFilter
{
  public Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
  {
    foreach (var arg in context.ActionArguments.Values.Where(a => a is not null))
    {
      var validator = services.GetService(typeof(IRequestValidator<>).MakeGenericType(arg!.GetType()));
      if (validator is null) continue;
      var method = validator.GetType().GetMethod(nameof(IRequestValidator<object>.Validate))!;
      var result = (RoadMate.Domain.Common.Result)method.Invoke(validator, [arg, clock.GetUtcNow()])!;
      if (result.IsFailure)
      {
        context.Result = result.Error.ToProblem(context.HttpContext);
        return Task.CompletedTask;
      }
    }
    return next();
  }
}
