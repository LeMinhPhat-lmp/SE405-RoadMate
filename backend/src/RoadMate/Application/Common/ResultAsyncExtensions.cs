using RoadMate.Domain.Common;

namespace RoadMate.Application.Common;

/// <summary>Async railway helpers (only where they shorten the code without hiding flow; ≤ 3 operators per expression).</summary>
public static class ResultAsyncExtensions
{
  public static async Task<Result<TOut>> MapAsync<TIn, TOut>(this Task<Result<TIn>> task, Func<TIn, TOut> map)
  {
    var r = await task;
    return r.IsFailure ? r.Error : map(r.Value);
  }

  public static async Task<Result<TOut>> BindAsync<TIn, TOut>(this Task<Result<TIn>> task, Func<TIn, Task<Result<TOut>>> next)
  {
    var r = await task;
    return r.IsFailure ? r.Error : await next(r.Value);
  }

  public static async Task<Result<T>> TapAsync<T>(this Task<Result<T>> task, Func<T, Task> action)
  {
    var r = await task;
    if (r.IsSuccess) await action(r.Value);
    return r;
  }
}
