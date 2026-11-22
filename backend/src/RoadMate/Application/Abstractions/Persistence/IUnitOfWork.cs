namespace RoadMate.Application.Abstractions.Persistence;

/// <summary>Commit boundary only (ISP). Repositories are injected individually; services never reach them through the UoW.</summary>
public interface IUnitOfWork
{
  /// <summary>Dispatches pending domain events BEFORE committing (handlers' changes join the same transaction), then saves once.</summary>
  Task<int> SaveChangesAsync(CancellationToken ct = default);
  Task ExecuteInTransactionAsync(Func<CancellationToken, Task> action, CancellationToken ct = default);
  /// <summary>Forget all tracked changes (after a unique-violation race or a failed unit of work).</summary>
  void DiscardChanges();
}
