using RoadMate.Application.Abstractions.Persistence;
using RoadMate.Domain.Common;

namespace RoadMate.Application.Common;

/// <summary>Retries a unit of work when the xmin concurrency token says someone else won; the loser reloads fresh state.</summary>
public static class OptimisticRetry
{
  public static readonly Error Conflict = Error.Conflict("CONCURRENCY_CONFLICT", "Dữ liệu vừa được thay đổi, hãy thử lại.");

  public static async Task<Result<T>> RunAsync<T>(IUnitOfWork uow, Func<CancellationToken, Task<Result<T>>> op, int maxAttempts = 3, CancellationToken ct = default)
  {
    for (var attempt = 1; ; attempt++)
    {
      try { return await op(ct); }
      catch (ConcurrencyConflictException)
      {
        uow.DiscardChanges();
        if (attempt >= maxAttempts) return Conflict;
      }
    }
  }

  public static async Task<Result> RunAsync(IUnitOfWork uow, Func<CancellationToken, Task<Result>> op, int maxAttempts = 3, CancellationToken ct = default)
  {
    for (var attempt = 1; ; attempt++)
    {
      try { return await op(ct); }
      catch (ConcurrencyConflictException)
      {
        uow.DiscardChanges();
        if (attempt >= maxAttempts) return Conflict;
      }
    }
  }
}
