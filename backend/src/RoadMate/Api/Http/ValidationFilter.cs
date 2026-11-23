using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace RoadMate.Api.Http;

/// <summary>
/// Runs every registered FluentValidation <c>IValidator&lt;T&gt;</c> asynchronously for non-null action arguments
/// before the controller executes and returns 400 ProblemDetails on failure. Controllers never validate by hand.
/// </summary>
public sealed class ValidationFilter : IAsyncActionFilter
{
  public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
  {
    var services = context.HttpContext.RequestServices;
    var ct = context.HttpContext.RequestAborted;
    List<ValidationFailure>? failures = null;

    foreach (var arg in context.ActionArguments.Values)
    {
      if (arg is null or CancellationToken) continue;

      var validatorType = typeof(IValidator<>).MakeGenericType(arg.GetType());
      var validators = services.GetServices(validatorType);
      ValidationContext<object>? validationContext = null;

      foreach (var item in validators)
      {
        if (item is not IValidator validator) continue;
        validationContext ??= new ValidationContext<object>(arg);
        var result = await validator.ValidateAsync(validationContext, ct);
        if (!result.IsValid)
        {
          failures ??= [];
          failures.AddRange(result.Errors);
        }
      }
    }

    if (failures is { Count: > 0 })
    {
      var first = failures[0];
      var code = string.IsNullOrWhiteSpace(first.ErrorCode) ? "VALIDATION_ERROR" : first.ErrorCode;
      var pd = ProblemFactory.Create(context.HttpContext, StatusCodes.Status400BadRequest, code, first.ErrorMessage);
      pd.Extensions["errors"] = failures
        .GroupBy(f => f.PropertyName)
        .ToDictionary(g => g.Key, g => g.Select(f => f.ErrorMessage).Distinct().ToArray());
      context.Result = new ObjectResult(pd) { StatusCode = pd.Status };
      return;
    }

    await next();
  }
}
