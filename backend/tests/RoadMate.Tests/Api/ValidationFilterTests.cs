using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using RoadMate.Api.Http;

namespace RoadMate.Tests.Api;

public class ValidationFilterTests
{
  private sealed record AsyncSampleRequest(string Value);

  private sealed class AsyncSampleValidator : AbstractValidator<AsyncSampleRequest>
  {
    public AsyncSampleValidator()
    {
      RuleFor(x => x.Value)
        .MustAsync(async (val, ct) =>
        {
          await Task.Yield();
          ct.ThrowIfCancellationRequested();
          return val != "forbidden";
        })
        .WithErrorCode("ASYNC_REJECTED")
        .WithMessage("Giá trị không hợp lệ (async).");
    }
  }

  private static ActionExecutingContext CreateContext(IServiceProvider services, IDictionary<string, object?> args, CancellationToken ct = default)
  {
    var http = new DefaultHttpContext { RequestServices = services, RequestAborted = ct };
    http.Items[CorrelationKeys.ItemKey] = "corr-123";
    var action = new ActionContext(http, new RouteData(), new ActionDescriptor());
    return new ActionExecutingContext(action, [], args, controller: new object());
  }

  [Fact]
  public async Task Invalid_request_is_rejected_with_400_problem_details_before_action_executes()
  {
    var sc = new ServiceCollection();
    sc.AddScoped<IValidator<CreateAlertRequest>, CreateAlertRequestValidator>();
    await using var sp = sc.BuildServiceProvider();
    using var scope = sp.CreateScope();

    var ctx = CreateContext(scope.ServiceProvider, new Dictionary<string, object?>
    {
      ["req"] = new CreateAlertRequest("", "boom", 91, 0, null)
    });

    var nextCalled = false;
    await new ValidationFilter().OnActionExecutionAsync(ctx, () =>
    {
      nextCalled = true;
      return Task.FromResult<ActionExecutedContext>(null!);
    });

    Assert.False(nextCalled);
    var obj = Assert.IsType<ObjectResult>(ctx.Result);
    Assert.Equal(StatusCodes.Status400BadRequest, obj.StatusCode);
    var pd = Assert.IsType<ProblemDetails>(obj.Value);
    Assert.Equal("INVALID_CLIENT_ID", pd.Title);
    Assert.Equal("INVALID_CLIENT_ID", pd.Extensions["code"]);
    Assert.Equal("corr-123", pd.Extensions["correlationId"]);
    var errors = Assert.IsType<Dictionary<string, string[]>>(pd.Extensions["errors"]);
    Assert.True(errors.ContainsKey(nameof(CreateAlertRequest.ClientId)));
    Assert.True(errors.ContainsKey(nameof(CreateAlertRequest.Type)));
    Assert.True(errors.ContainsKey("Location"));
  }

  [Fact]
  public async Task Valid_request_passes_through_and_executes_action()
  {
    var sc = new ServiceCollection();
    sc.AddScoped<IValidator<ContactRequest>, ContactRequestValidator>();
    await using var sp = sc.BuildServiceProvider();
    using var scope = sp.CreateScope();

    var ctx = CreateContext(scope.ServiceProvider, new Dictionary<string, object?>
    {
      ["id"] = Guid.NewGuid(),
      ["req"] = new ContactRequest("Mẹ", "0912345678", null, null),
      ["optional"] = null,
      ["ct"] = CancellationToken.None
    });

    var nextCalled = false;
    await new ValidationFilter().OnActionExecutionAsync(ctx, () =>
    {
      nextCalled = true;
      return Task.FromResult<ActionExecutedContext>(null!);
    });

    Assert.True(nextCalled);
    Assert.Null(ctx.Result);
  }

  [Fact]
  public async Task Async_validator_executes_asynchronously_and_honors_cancellation()
  {
    var sc = new ServiceCollection();
    sc.AddScoped<IValidator<AsyncSampleRequest>, AsyncSampleValidator>();
    await using var sp = sc.BuildServiceProvider();
    using var scope = sp.CreateScope();

    // Invalid async rule rejects before action
    var failCtx = CreateContext(scope.ServiceProvider, new Dictionary<string, object?> { ["req"] = new AsyncSampleRequest("forbidden") });
    var nextCalled = false;
    await new ValidationFilter().OnActionExecutionAsync(failCtx, () => { nextCalled = true; return Task.FromResult<ActionExecutedContext>(null!); });
    Assert.False(nextCalled);
    var pd = Assert.IsType<ProblemDetails>(Assert.IsType<ObjectResult>(failCtx.Result).Value);
    Assert.Equal("ASYNC_REJECTED", pd.Extensions["code"]);

    // Valid async rule continues
    var okCtx = CreateContext(scope.ServiceProvider, new Dictionary<string, object?> { ["req"] = new AsyncSampleRequest("ok") });
    await new ValidationFilter().OnActionExecutionAsync(okCtx, () => { nextCalled = true; return Task.FromResult<ActionExecutedContext>(null!); });
    Assert.True(nextCalled);

    // Cancelled token propagates
    using var cts = new CancellationTokenSource();
    cts.Cancel();
    var cancelCtx = CreateContext(scope.ServiceProvider, new Dictionary<string, object?> { ["req"] = new AsyncSampleRequest("ok") }, cts.Token);
    await Assert.ThrowsAsync<OperationCanceledException>(() =>
      new ValidationFilter().OnActionExecutionAsync(cancelCtx, () => Task.FromResult<ActionExecutedContext>(null!)));
  }
}
