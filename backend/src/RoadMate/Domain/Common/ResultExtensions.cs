namespace RoadMate.Domain.Common;

/// <summary>Railway helpers that remove <c>if (x.IsFailure) return x.Error;</c> boilerplate. Failure short-circuits, error is preserved.</summary>
public static class ResultExtensions
{
  public static Result<TOut> Map<TIn, TOut>(this Result<TIn> r, Func<TIn, TOut> map) =>
    r.IsFailure ? r.Error : map(r.Value);

  public static Result<TOut> Bind<TIn, TOut>(this Result<TIn> r, Func<TIn, Result<TOut>> next) =>
    r.IsFailure ? r.Error : next(r.Value);

  public static Result Bind<TIn>(this Result<TIn> r, Func<TIn, Result> next) =>
    r.IsFailure ? r.Error : next(r.Value);

  public static Result<T> Ensure<T>(this Result<T> r, Func<T, bool> predicate, Error error) =>
    r.IsFailure ? r : predicate(r.Value) ? r : error;

  public static Result<T> Tap<T>(this Result<T> r, Action<T> action)
  {
    if (r.IsSuccess) action(r.Value);
    return r;
  }

  public static TOut Match<TIn, TOut>(this Result<TIn> r, Func<TIn, TOut> ok, Func<Error, TOut> fail) =>
    r.IsSuccess ? ok(r.Value) : fail(r.Error);

  public static Result OnFailure(this Result r, Action<Error> action)
  {
    if (r.IsFailure) action(r.Error);
    return r;
  }

  public static Result<T> ToResult<T>(this T? value, Error ifNull) where T : class => value is null ? ifNull : value;
}
