using Microsoft.EntityFrameworkCore;
using RoadMate.Application.Abstractions.Messaging;
using RoadMate.Application.Abstractions.Persistence;
using RoadMate.Application.Common;
using RoadMate.Domain.Abstractions;

namespace RoadMate.Infrastructure.Persistence.Repositories;

/// <summary>
/// Commit boundary. Domain events raised by tracked aggregates are dispatched BEFORE the commit (handlers' changes join the same transaction);
/// handlers may raise new events, so dispatching repeats (bounded) until none are pending. Provider exceptions are translated to neutral ones.
/// </summary>
public class UnitOfWork(AppDbContext db, IDomainEventDispatcher dispatcher) : IUnitOfWork
{
  private const int MaxDispatchRounds = 5;

  public async Task<int> SaveChangesAsync(CancellationToken ct = default)
  {
    try
    {
      await DispatchPendingEventsAsync(ct);
      return await db.SaveChangesAsync(ct);
    }
    catch (DbUpdateConcurrencyException ex) { throw new ConcurrencyConflictException(ex); }
    catch (DbUpdateException ex) when (ex.IsUniqueViolation()) { throw new UniqueViolationException(null, ex); }
    catch (DbUpdateException ex) when (ex.IsForeignKeyViolation()) { throw new ForeignKeyViolationException(ex); }
  }

  public async Task ExecuteInTransactionAsync(Func<CancellationToken, Task> action, CancellationToken ct = default)
  {
    var strategy = db.Database.CreateExecutionStrategy();
    await strategy.ExecuteAsync(async () =>
    {
      await using var tx = await db.Database.BeginTransactionAsync(ct);
      await action(ct);
      await tx.CommitAsync(ct);
    });
  }

  public void DiscardChanges() => db.ChangeTracker.Clear();

  private async Task DispatchPendingEventsAsync(CancellationToken ct)
  {
    for (var round = 0; round < MaxDispatchRounds; round++)
    {
      db.ChangeTracker.DetectChanges();
      var holders = db.ChangeTracker.Entries<IHasDomainEvents>().Select(e => e.Entity).Where(e => e.DomainEvents.Count > 0).ToList();
      if (holders.Count == 0) return;
      var events = holders.SelectMany(h => h.DomainEvents).ToList();
      foreach (var h in holders) h.ClearDomainEvents();
      await dispatcher.DispatchAsync(events, ct);
    }
    throw new InvalidOperationException($"Sự kiện domain chưa xử lý xong sau {MaxDispatchRounds} vòng (có thể đang lặp vô hạn).");
  }
}
